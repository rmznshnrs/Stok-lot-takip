using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class SevkiyatServisi(AppDbContext db) : ISevkiyatServisi
{
    public async Task<List<SevkiyatDto>> ListeAsync(string? arama = null, string? durum = null, int? musteriId = null,
                                                    CancellationToken ct = default, DateOnly? baslangic = null)
    {
        var q = Sorgu();
        if (baslangic is { } b)
            q = q.Where(s => s.Tarih >= b);
        if (musteriId is { } m)
            q = q.Where(s => s.MusteriId == m);
        if (!string.IsNullOrWhiteSpace(durum))
        {
            if (!Enum.TryParse<SevkiyatDurumu>(durum, ignoreCase: true, out var d) || !Enum.IsDefined(d))
                throw new IsKuraliHatasi("Durum Hazirlaniyor, Kargoda veya TeslimEdildi olmalı.");
            q = q.Where(s => s.Durum == d);
        }
        arama = arama?.Trim();
        if (!string.IsNullOrEmpty(arama))
        {
            var desen = Arama.Desen(arama);
            q = q.Where(s => EF.Functions.Like(s.No, desen) || EF.Functions.Like(s.Musteri!.Ad, desen)
                             || EF.Functions.Like(s.TakipNo, desen)
                             || (s.Siparis != null && EF.Functions.Like(s.Siparis.SiparisNo, desen))
                             || s.Kalemler.Any(k => EF.Functions.Like(k.Uretim!.SeriNo, desen))
                             || s.Iadeler.Any(i => EF.Functions.Like(i.Uretim!.SeriNo, desen)));
        }
        var sirali = q.OrderByDescending(s => s.TeslimZamani ?? s.KargoZamani ?? s.Zaman).ThenByDescending(s => s.Tarih).ThenByDescending(s => s.Id);
        var liste = await (baslangic is null ? sirali.Take(500) : sirali).ToListAsync(ct);
        return [.. liste.Select(Donustur)];
    }

    public async Task<SevkiyatDto> GetirAsync(int id, CancellationToken ct = default) =>
        Donustur(await Sorgu().SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new BulunamadiHatasi("Sevkiyat bulunamadı."));

    public Task<SevkiyatDto> OlusturAsync(SevkiyatIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var seriler = SeriListesi.Temizle(istek.SeriNolar);
        if (seriler.Count == 0)
            throw new IsKuraliHatasi("Gönderilecek ürünleri seçin.");

        return Islem.CalistirAsync(db, async () =>
        {
            var siparis = await db.Siparisler.Include(s => s.Musteri)
                              .Include(s => s.Kalemler).ThenInclude(k => k.Ayrilanlar).ThenInclude(u => u.SatisKalemi)
                              .AsSplitQuery().SingleOrDefaultAsync(s => s.Id == istek.SiparisId, ct)
                          ?? throw new BulunamadiHatasi("Talep bulunamadı.");
            if (siparis.IptalZamani is not null)
                throw new IsKuraliHatasi("Talep iptal edilmiş.");
            var bekleyen = siparis.Kalemler.SelectMany(k => k.Ayrilanlar).Where(u => u.SatisKalemi is null)
                .Select(u => u.SeriNo).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var uygunDegil = seriler.Where(s => !bekleyen.Contains(s)).ToList();
            if (uygunDegil.Count > 0)
                throw new IsKuraliHatasi($"Bu talebe ayrılmamış ya da zaten gönderilmiş: {string.Join(", ", uygunDegil)}");

            var sonuc = await new SatisServisi(db).SatAsync(
                new SatisIstek(siparis.Musteri!.Ad, seriler, GeriCagrilanOnayi: istek.GeriCagrilanOnayi), kullaniciId, ct);
            var sevkiyat = await db.Satislar.SingleAsync(s => s.Id == sonuc.SatisId, ct);
            sevkiyat.SiparisId = siparis.Id;
            sevkiyat.Aciklama = (istek.Aciklama ?? "").Trim();
            sevkiyat.No = null;
            KargoYaz(sevkiyat, istek.KargoFirmasi, istek.TakipNo, zorunlu: false);
            sevkiyat.No = await NoKontrolAsync(sevkiyat.Id, istek.SevkiyatNo, ct);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(sevkiyat.Id, ct);
        }, ct);
    }

    public Task<SevkiyatDto> KargoAsync(int id, KargoIstek istek, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var s = await db.Satislar.Include(x => x.Kalemler).ThenInclude(k => k.Uretim)
                        .SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BulunamadiHatasi("Sevkiyat bulunamadı.");
            var hazirlaniyordu = s.Durum == SevkiyatDurumu.Hazirlaniyor;
            List<SatisKalemi> kalanlar = [];
            if (hazirlaniyordu && istek.SeriNolar is { Count: > 0 })
            {
                var secilen = SeriListesi.Temizle(istek.SeriNolar).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var yabanci = secilen.Where(x => !s.Kalemler.Any(k => string.Equals(k.Uretim!.SeriNo, x, StringComparison.OrdinalIgnoreCase))).ToList();
                if (yabanci.Count > 0)
                    throw new IsKuraliHatasi($"Bu sevkiyatta olmayan seri no: {string.Join(", ", yabanci)}");
                kalanlar = [.. s.Kalemler.Where(k => !secilen.Contains(k.Uretim!.SeriNo))];
            }
            KargoYaz(s, istek.KargoFirmasi, istek.TakipNo, zorunlu: true);
            if (istek.SevkiyatNo is not null)
                s.No = await NoKontrolAsync(id, istek.SevkiyatNo, ct);
            if (kalanlar.Count > 0)
            {
                var uretimIdleri = kalanlar.Select(k => k.UretimId).ToList();
                db.SatisKalemleri.RemoveRange(kalanlar);
                await db.SaveChangesAsync(ct);
                db.Satislar.Add(new Satis
                {
                    MusteriId = s.MusteriId, SiparisId = s.SiparisId, Tarih = s.Tarih, Zaman = s.Zaman, Durum = SevkiyatDurumu.Hazirlaniyor,
                    Aciklama = s.Aciklama, OlusturanKullaniciId = s.OlusturanKullaniciId, OlusturmaZamani = s.OlusturmaZamani,
                    Kalemler = [.. uretimIdleri.Select(u => new SatisKalemi { UretimId = u })],
                });
            }
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    internal async Task BekleyenleriAlAsync(int siparisId, int kullaniciId, CancellationToken ct)
    {
        var seriler = await db.Uretimler.Where(u => u.SiparisKalemi!.SiparisId == siparisId && u.SatisKalemi == null)
            .Select(u => u.SeriNo).ToListAsync(ct);
        if (seriler.Count == 0)
            return;
        try
        {
            var yeni = await OlusturAsync(new SevkiyatIstek(siparisId, seriler), kullaniciId, ct);
            await BekleyenleBirlestirAsync(yeni.Id, ct);
        }
        catch (SatisHatasi)
        {
        }
    }

    internal async Task<SevkiyatDto> BekleyenleBirlestirAsync(int id, CancellationToken ct)
    {
        var yeni = await db.Satislar.Include(s => s.Kalemler).SingleAsync(s => s.Id == id, ct);
        var eski = await db.Satislar.Include(s => s.Kalemler)
            .Where(s => s.Id != id && s.SiparisId != null && s.SiparisId == yeni.SiparisId
                        && s.Durum == SevkiyatDurumu.Hazirlaniyor && s.TakipNo == "")
            .OrderBy(s => s.Id).FirstOrDefaultAsync(ct);
        if (eski is null)
            return await GetirAsync(id, ct);
        var uretimIdleri = yeni.Kalemler.Select(k => k.UretimId).ToList();
        db.SatisKalemleri.RemoveRange(yeni.Kalemler);
        db.Satislar.Remove(yeni);
        await db.SaveChangesAsync(ct);
        foreach (var u in uretimIdleri)
            eski.Kalemler.Add(new SatisKalemi { UretimId = u });
        await db.SaveChangesAsync(ct);
        return await GetirAsync(eski.Id, ct);
    }

    public Task<SevkiyatDto> TeslimAsync(int id, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var s = await db.Satislar.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BulunamadiHatasi("Sevkiyat bulunamadı.");
            if (s.Durum == SevkiyatDurumu.TeslimEdildi)
                throw new IsKuraliHatasi("Sevkiyat zaten teslim edilmiş.");
            s.Durum = SevkiyatDurumu.TeslimEdildi;
            s.TeslimZamani = Zaman.Simdi();
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    public Task<SevkiyatDto> NoAsync(int id, SevkiyatNoIstek istek, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var s = await db.Satislar.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BulunamadiHatasi("Sevkiyat bulunamadı.");
            s.No = await NoKontrolAsync(id, istek.No, ct);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    private async Task<string?> NoKontrolAsync(int id, string? no, CancellationToken ct)
    {
        no = (no ?? "").Trim();
        if (no.Length == 0)
            return null;
        if (no.Length > 20)
            throw new IsKuraliHatasi("Sevkiyat no en fazla 20 karakter olabilir.");
        if (await db.Satislar.AnyAsync(x => x.No == no && x.Id != id, ct))
            throw new IsKuraliHatasi("Bu sevkiyat no başka bir sevkiyatta kullanılıyor.");
        return no;
    }

    private static void KargoYaz(Satis s, string? firma, string? takipNo, bool zorunlu)
    {
        firma = (firma ?? "").Trim();
        takipNo = (takipNo ?? "").Trim();
        if (zorunlu && (firma.Length == 0 || takipNo.Length == 0))
            throw new IsKuraliHatasi("Kargo firması ve takip no girin.");
        if (takipNo.Length > 0 && firma.Length == 0)
            throw new IsKuraliHatasi("Kargo firmasını girin.");
        if (firma.Length > 100 || takipNo.Length > 100)
            throw new IsKuraliHatasi("Kargo firması ve takip no en fazla 100 karakter olabilir.");
        s.KargoFirmasi = firma;
        s.TakipNo = takipNo;
        if (takipNo.Length > 0 && s.Durum == SevkiyatDurumu.Hazirlaniyor)
        {
            s.Durum = SevkiyatDurumu.Kargoda;
            s.KargoZamani = Zaman.Simdi();
        }
    }

    private IQueryable<Satis> Sorgu() => db.Satislar.AsNoTracking()
        .Include(s => s.Musteri).Include(s => s.Siparis).Include(s => s.OlusturanKullanici)
        .Include(s => s.Kalemler).ThenInclude(k => k.Uretim).ThenInclude(u => u!.Urun)
        .Include(s => s.Iadeler).ThenInclude(i => i.Uretim)
        .AsSplitQuery();

    private static SevkiyatDto Donustur(Satis s) => new(
        s.Id, s.No, s.MusteriId, s.Musteri!.Ad, s.SiparisId, s.Siparis is { SiparisNo.Length: > 0 } ? s.Siparis.SiparisNo : null, s.Tarih, s.Zaman, s.Durum.ToString(),
        s.KargoFirmasi, s.TakipNo, s.Aciklama, s.KargoZamani, s.TeslimZamani, s.OlusturmaZamani,
        $"{s.OlusturanKullanici!.Ad} {s.OlusturanKullanici.Soyad}",
        [.. s.Kalemler.Select(k => new SevkiyatKalemiDto(k.Uretim!.SeriNo, k.Uretim.Urun!.Ad)).OrderBy(k => k.SeriNo)],
        [.. s.Iadeler.Select(i => i.Uretim!.SeriNo).Distinct().Order()]);
}
