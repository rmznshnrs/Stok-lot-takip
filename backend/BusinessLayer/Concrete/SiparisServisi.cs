using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class SiparisServisi(AppDbContext db) : ISiparisServisi
{
    public const int EnFazlaAdet = 999;
    private const string NoOneki = "SP-";

    public async Task<List<SiparisDto>> ListeAsync(int? musteriId = null, bool acik = false, CancellationToken ct = default,
                                                   DateOnly? baslangic = null)
    {
        var q = Sorgu();
        if (baslangic is { } b)
            q = q.Where(s => s.Tarih >= b);
        if (musteriId is { } m)
            q = q.Where(s => s.MusteriId == m);
        if (acik)
            q = q.Where(s => s.IptalZamani == null
                             && s.Kalemler.Any(k => k.Ayrilanlar.Count(u => u.SatisKalemi != null) < k.Adet));
        var sirali = q.OrderByDescending(s => s.Tarih).ThenByDescending(s => s.Id);
        var liste = await (baslangic is null ? sirali.Take(500) : sirali).ToListAsync(ct);
        return [.. liste.Select(Donustur)];
    }

    public async Task<SiparisDto> GetirAsync(int id, CancellationToken ct = default) =>
        Donustur(await Sorgu().SingleOrDefaultAsync(s => s.Id == id, ct) ?? throw new BulunamadiHatasi("Talep bulunamadı."));

    public async Task<List<SiparisUrunuDto>> UrunlerAsync(int id, CancellationToken ct = default)
    {
        if (!await db.Siparisler.AnyAsync(s => s.Id == id, ct))
            throw new BulunamadiHatasi("Talep bulunamadı.");
        var emirler = await db.UretimEmirleri.AsNoTracking().Where(e => e.SiparisKalemi!.SiparisId == id).OrderBy(e => e.PlanlananTarih)
            .Select(e => new { e.Id, e.Durum, e.Adet, e.Uretilen, e.PlanlananTarih, e.BaslamaZamani, UrunAd = e.Urun!.Ad }).ToListAsync(ct);
        var urunler = await db.Uretimler.AsNoTracking().Where(u => u.SiparisKalemi!.SiparisId == id)
            .Select(u => new
            {
                u.SeriNo, UrunAd = u.Urun!.Ad, u.Tarih, Hurda = u.Hurda != null,
                Satis = u.SatisKalemi == null ? null : new
                {
                    u.SatisKalemi.Satis!.Id, u.SatisKalemi.Satis.No, u.SatisKalemi.Satis.Durum,
                    u.SatisKalemi.Satis.KargoFirmasi, u.SatisKalemi.Satis.TakipNo, u.SatisKalemi.Satis.KargoZamani, u.SatisKalemi.Satis.TeslimZamani,
                },
            }).ToListAsync(ct);

        var satirlar = urunler.OrderBy(u => u.Satis?.KargoZamani ?? DateTimeOffset.MaxValue).ThenBy(u => u.Tarih)
            .ThenBy(u => u.SeriNo, StringComparer.Ordinal)
            .Select(u => new SiparisUrunuDto(
                u.Hurda ? "hurda" : u.Satis?.Durum.ToString() ?? "depoda", u.UrunAd, 1, u.SeriNo,
                null, u.Tarih,
                u.Satis?.Id, u.Satis?.No, u.Satis?.KargoFirmasi ?? "", u.Satis?.TakipNo ?? "", u.Satis?.KargoZamani, u.Satis?.TeslimZamani));
        var bekleyen = emirler.Where(e => e.Durum == UretimEmriDurumu.Bekliyor && e.Adet > e.Uretilen)
            .Select(e => new SiparisUrunuDto(e.BaslamaZamani is not null || e.Uretilen > 0 ? "uretimde" : "sirada",
                e.UrunAd, e.Adet - e.Uretilen, null, e.PlanlananTarih, null, null, null, "", "", null, null));
        return [.. satirlar.Concat(bekleyen)];
    }

    public Task<SiparisDto> SiparisNoAsync(int id, SiparisNoIstek istek, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var s = await db.Siparisler.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw new BulunamadiHatasi("Talep bulunamadı.");
            s.SiparisNo = SiparisNoTemizle(istek.SiparisNo);
            await SiparisNoBenzersizAsync(s.SiparisNo, s.Id, ct);
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    private async Task SiparisNoBenzersizAsync(string no, int haricId, CancellationToken ct)
    {
        if (no.Length > 0 && await db.Siparisler.AnyAsync(x => x.SiparisNo == no && x.Id != haricId, ct))
            throw new IsKuraliHatasi($"{no} sipariş no başka bir talepte kullanılıyor.");
    }

    private static string SiparisNoTemizle(string? no)
    {
        no = (no ?? "").Trim();
        if (no.Length > 50)
            throw new IsKuraliHatasi("Sipariş no en fazla 50 karakter olabilir.");
        return no;
    }

    public Task<SiparisDto> OlusturAsync(SiparisIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var kalemler = istek.Kalemler ?? [];
        if (kalemler.Count == 0)
            throw new IsKuraliHatasi("En az bir ürün ekleyin.");
        if (kalemler.Any(k => k.Adet < 1 || k.Adet > EnFazlaAdet))
            throw new IsKuraliHatasi($"Adet 1 ile {EnFazlaAdet} arasında olmalı.");
        if (kalemler.GroupBy(k => k.UrunId).Any(g => g.Count() > 1))
            throw new IsKuraliHatasi("Aynı ürün talepte iki kez yazılamaz.");

        return Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var musteri = await db.Musteriler.SingleOrDefaultAsync(m => m.Id == istek.MusteriId, ct)
                          ?? throw new BulunamadiHatasi("Müşteri bulunamadı.");
            var urunIdleri = kalemler.Select(k => k.UrunId).ToList();
            var urunler = await db.Urunler.Where(u => urunIdleri.Contains(u.Id)).ToListAsync(ct);
            if (urunler.Count != urunIdleri.Count)
                throw new BulunamadiHatasi("Ürün bulunamadı.");

            var mevcut = await db.Siparisler.Where(s => s.No.StartsWith(NoOneki)).Select(s => s.No).ToListAsync(ct);
            var siparis = new Siparis
            {
                No = $"{NoOneki}{Sira.EnBuyuk(mevcut, NoOneki) + 1:D4}", Musteri = musteri,
                Tarih = istek.Tarih ?? Zaman.Bugun(), IstenenTarih = istek.IstenenTarih,
                Aciklama = (istek.Aciklama ?? "").Trim(), SiparisNo = SiparisNoTemizle(istek.SiparisNo),
                OlusturanKullanici = kullanici, OlusturmaZamani = Zaman.Simdi(),
            };
            await SiparisNoBenzersizAsync(siparis.SiparisNo, 0, ct);
            foreach (var k in kalemler)
            {
                var kalem = new SiparisKalemi
                {
                    Siparis = siparis, UrunId = k.UrunId, Urun = urunler.First(u => u.Id == k.UrunId), Adet = k.Adet,
                };
                siparis.Kalemler.Add(kalem);
                await AyirAsync(kalem, k.SeriNolar, ct);
            }
            db.Siparisler.Add(siparis);
            await db.SaveChangesAsync(ct);
            await new SevkiyatServisi(db).BekleyenleriAlAsync(siparis.Id, kullanici.Id, ct);
            return await GetirAsync(siparis.Id, ct);
        }, ct);
    }

    public Task<SiparisDto> SeriAyirAsync(int kalemId, SeriAyirIstek istek, CancellationToken ct = default, int? kullaniciId = null) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kalem = await KalemGetirAsync(kalemId, ct);
            if (SeriListesi.Temizle(istek.SeriNolar).Count == 0)
                throw new IsKuraliHatasi("Seri no seçin.");
            await AyirAsync(kalem, istek.SeriNolar, ct);
            await db.SaveChangesAsync(ct);
            await new SevkiyatServisi(db).BekleyenleriAlAsync(kalem.SiparisId, kullaniciId ?? kalem.Siparis!.OlusturanKullaniciId, ct);
            return await GetirAsync(kalem.SiparisId, ct);
        }, ct);

    public Task<SiparisDto> SeriBirakAsync(int kalemId, string seriNo, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var kalem = await KalemGetirAsync(kalemId, ct);
            var u = kalem.Ayrilanlar.SingleOrDefault(x => string.Equals(x.SeriNo, (seriNo ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
                    ?? throw new BulunamadiHatasi($"{seriNo} bu kaleme ayrılmamış.");
            if (u.SatisKalemi is not null)
            {
                var sevkiyat = await db.Satislar.Include(s => s.Kalemler).SingleAsync(s => s.Id == u.SatisKalemi.SatisId, ct);
                if (sevkiyat.Durum != SevkiyatDurumu.Hazirlaniyor || sevkiyat.TakipNo.Length > 0)
                    throw new IsKuraliHatasi($"{u.SeriNo} kargoya verilmiş; ayrımı kaldırılamaz.");
                db.SatisKalemleri.Remove(u.SatisKalemi);
                if (sevkiyat.Kalemler.Count == 1)
                    db.Satislar.Remove(sevkiyat);
            }
            u.SiparisKalemiId = null;
            await db.SaveChangesAsync(ct);
            return await GetirAsync(kalem.SiparisId, ct);
        }, ct);

    public Task<SiparisDto> IptalAsync(int id, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var siparis = await db.Siparisler
                              .Include(s => s.Kalemler).ThenInclude(k => k.Ayrilanlar).ThenInclude(u => u.SatisKalemi)
                              .Include(s => s.Kalemler).ThenInclude(k => k.UretimEmirleri)
                              .AsSplitQuery().SingleOrDefaultAsync(s => s.Id == id, ct)
                          ?? throw new BulunamadiHatasi("Talep bulunamadı.");
            if (siparis.IptalZamani is not null)
                throw new IsKuraliHatasi("Talep zaten iptal edilmiş.");
            var bekleyenler = await db.Satislar.Include(x => x.Kalemler)
                .Where(x => x.SiparisId == id && x.Durum == SevkiyatDurumu.Hazirlaniyor && x.TakipNo == "").ToListAsync(ct);
            foreach (var b in bekleyenler)
            {
                db.SatisKalemleri.RemoveRange(b.Kalemler);
                db.Satislar.Remove(b);
            }
            var bekleyenUretimler = bekleyenler.SelectMany(b => b.Kalemler).Select(k => k.UretimId).ToHashSet();
            foreach (var kalem in siparis.Kalemler)
                foreach (var u in kalem.Ayrilanlar.Where(u => bekleyenUretimler.Contains(u.Id)))
                    u.SatisKalemi = null;
            foreach (var kalem in siparis.Kalemler)
            {
                foreach (var u in kalem.Ayrilanlar.Where(u => u.SatisKalemi is null))
                    u.SiparisKalemiId = null;
                foreach (var e in kalem.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor))
                    UretimSirasiServisi.Kapat(e);
            }
            siparis.IptalZamani = Zaman.Simdi();
            await db.SaveChangesAsync(ct);
            return await GetirAsync(id, ct);
        }, ct);

    public Task<List<HazirSeriDto>> HazirSerilerAsync(int urunId, CancellationToken ct = default) =>
        Serbest(db.Uretimler.AsNoTracking().Where(u => u.UrunId == urunId))
            .OrderBy(u => u.SeriNo).Select(u => new HazirSeriDto(u.SeriNo, u.Tarih)).ToListAsync(ct);

    private static IQueryable<Uretim> Serbest(IQueryable<Uretim> q) =>
        q.Where(u => u.SatisKalemi == null && u.Hurda == null && u.SiparisKalemiId == null
                     && !u.GeriCagirmalar.Any(g => g.KapanmaZamani == null));

    private async Task<SiparisKalemi> KalemGetirAsync(int kalemId, CancellationToken ct)
    {
        var kalem = await db.SiparisKalemleri.Include(k => k.Siparis).Include(k => k.Ayrilanlar).ThenInclude(u => u.SatisKalemi)
                        .SingleOrDefaultAsync(k => k.Id == kalemId, ct)
                    ?? throw new BulunamadiHatasi("Talep kalemi bulunamadı.");
        if (kalem.Siparis!.IptalZamani is not null)
            throw new IsKuraliHatasi("Talep iptal edilmiş.");
        return kalem;
    }

    private async Task AyirAsync(SiparisKalemi kalem, IReadOnlyList<string>? seriNolar, CancellationToken ct)
    {
        var seriler = SeriListesi.Temizle(seriNolar);
        if (seriler.Count == 0)
            return;
        var kayitlar = await db.Uretimler.Where(u => seriler.Contains(u.SeriNo)).ToListAsync(ct);
        var bulunamayan = seriler.Where(s => !kayitlar.Any(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase))).ToList();
        if (bulunamayan.Count > 0)
            throw new IsKuraliHatasi($"Bulunamayan seri no: {string.Join(", ", bulunamayan)}");
        var baskaUrun = kayitlar.Where(u => u.UrunId != kalem.UrunId).Select(u => u.SeriNo).Order().ToList();
        if (baskaUrun.Count > 0)
            throw new IsKuraliHatasi($"Bu ürüne ait değil: {string.Join(", ", baskaUrun)}");
        var idler = kayitlar.Select(u => u.Id).ToList();
        var serbest = await Serbest(db.Uretimler.Where(u => idler.Contains(u.Id))).Select(u => u.Id).ToListAsync(ct);
        var uygunDegil = kayitlar.Where(u => !serbest.Contains(u.Id)).Select(u => u.SeriNo).Order().ToList();
        if (uygunDegil.Count > 0)
            throw new IsKuraliHatasi($"Depoda serbest değil (satılmış, ayrılmış, hurda ya da geri çağrılmış): {string.Join(", ", uygunDegil)}");
        if (kalem.Ayrilanlar.Count + kayitlar.Count > kalem.Adet)
            throw new IsKuraliHatasi($"Bu kaleme en fazla {kalem.Adet - kalem.Ayrilanlar.Count} ürün daha ayrılabilir.");
        foreach (var u in kayitlar)
            kalem.Ayrilanlar.Add(u);
    }

    private IQueryable<Siparis> Sorgu() => db.Siparisler.AsNoTracking()
        .Include(s => s.Musteri).Include(s => s.OlusturanKullanici)
        .Include(s => s.Kalemler).ThenInclude(k => k.Urun)
        .Include(s => s.Kalemler).ThenInclude(k => k.Ayrilanlar).ThenInclude(u => u.SatisKalemi).ThenInclude(sk => sk!.Satis)
        .Include(s => s.Kalemler).ThenInclude(k => k.UretimEmirleri)
        .AsSplitQuery();

    private static SiparisDto Donustur(Siparis s)
    {
        var kalemler = s.Kalemler.OrderBy(k => k.Urun!.Ad, StringComparer.CurrentCulture).Select(k => new SiparisKalemiDto(
            k.Id, k.UrunId, k.Urun!.Kod, k.Urun.Ad, k.Adet,
            [.. k.Ayrilanlar.Select(u => u.SeriNo).Order()],
            [.. k.Ayrilanlar.Where(u => u.SatisKalemi is not null).Select(u => u.SeriNo).Order()],
            k.UretimEmirleri.Where(e => e.Durum == UretimEmriDurumu.Bekliyor).Sum(e => e.Adet - e.Uretilen))).ToList();
        var kargolanan = s.Kalemler.Sum(k => k.Ayrilanlar.Count(u => u.SatisKalemi?.Satis is { } v && v.Durum != SevkiyatDurumu.Hazirlaniyor));
        var kargoyaVerilecek = s.Kalemler.Sum(k => k.Ayrilanlar.Count(u => u.SatisKalemi?.Satis is { Durum: SevkiyatDurumu.Hazirlaniyor }));
        return new SiparisDto(s.Id, s.No, s.MusteriId, s.Musteri!.Ad, s.Tarih, s.IstenenTarih, s.Aciklama, Durum(s, kalemler, kargolanan, kargoyaVerilecek),
            kalemler, s.OlusturmaZamani, $"{s.OlusturanKullanici!.Ad} {s.OlusturanKullanici.Soyad}", s.IptalZamani,
            s.SiparisNo.Length > 0 ? s.SiparisNo : null,
            s.Kalemler.SelectMany(k => k.Ayrilanlar).Select(u => u.SatisKalemi?.Satis)
                .Where(v => v is not null && v.Durum != SevkiyatDurumu.Hazirlaniyor)
                .Max(v => v!.TeslimZamani ?? v.KargoZamani ?? v.Zaman));
    }

    private static string Durum(Siparis s, List<SiparisKalemiDto> kalemler, int gonderilen, int kargoyaVerilecek)
    {
        if (s.IptalZamani is not null)
            return "iptal";
        var toplam = kalemler.Sum(k => k.Adet);
        if (gonderilen >= toplam)
            return "tamamlandi";
        if (gonderilen > 0)
            return "kismen_gonderildi";
        if (kargoyaVerilecek > 0)
            return "kargoya_verilecek";
        var ayrilan = kalemler.Sum(k => k.Ayrilanlar.Count);
        if (ayrilan >= toplam)
            return "hazir";
        if (ayrilan > 0)
            return "kismen_hazir";
        return kalemler.Any(k => k.Uretimde > 0) ? "uretimde" : "bekliyor";
    }
}
