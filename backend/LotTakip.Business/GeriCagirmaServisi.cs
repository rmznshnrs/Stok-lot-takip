using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

public interface IGeriCagirmaServisi
{
    /// <summary>
    /// Yalnız Admin: seri numaralı ürünleri geri çağırır (açıklama zorunlu; sebep lot verilebilir).
    /// Bulunamayan, hurdaya ayrılmış ya da zaten açık geri çağırması olan varsa hiçbiri çağrılmaz.
    /// </summary>
    Task<List<GeriCagirmaDto>> GeriCagirAsync(GeriCagirmaIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Yalnız Admin: müşterideki ürün geri alındı. Satış iadesi yapılır, ürün depoya döner (satışa kapalı kalır).</summary>
    Task<GeriCagirmaDto> GeriAlAsync(int id, int kullaniciId, CancellationToken ct = default);

    /// <summary>Yalnız Admin: depodaki geri çağrılmış ürün yeniden satışa açılır; geri çağırma kapanır.</summary>
    Task<GeriCagirmaDto> SatisaAcAsync(int id, int kullaniciId, CancellationToken ct = default);

    /// <summary>Geri çağırmalar (yeniden eskiye). acik=true: yalnız sonuçlanmamışlar (müşteride, depoda, geri alındı).</summary>
    Task<List<GeriCagirmaDto>> ListeAsync(bool acik = false, string? arama = null, CancellationToken ct = default);
}

public class GeriCagirmaServisi(AppDbContext db) : IGeriCagirmaServisi
{
    public Task<List<GeriCagirmaDto>> GeriCagirAsync(GeriCagirmaIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var aciklama = (istek.Aciklama ?? "").Trim();
        if (aciklama.Length == 0)
            throw new IsKuraliHatasi("Geri çağırmada açıklama zorunludur.");
        var seriler = (istek.SeriNolar ?? []).Select(s => (s ?? "").Trim()).Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (seriler.Count == 0)
            throw new IsKuraliHatasi("Geri çağrılacak en az bir seri no seçilmeli.");

        return Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Ürün geri çağırma", ct);
            int? lotId = null;
            if (!string.IsNullOrWhiteSpace(istek.LotNo))
            {
                var no = istek.LotNo.Trim();
                lotId = await db.StokLotlari.Where(l => l.LotNo == no).Select(l => (int?)l.Id).SingleOrDefaultAsync(ct)
                        ?? throw new BulunamadiHatasi($"{no} lotu bulunamadı.");
            }
            var uretimler = await db.Uretimler.Include(u => u.Hurda).Include(u => u.GeriCagirmalar)
                .Where(u => seriler.Contains(u.SeriNo)).ToListAsync(ct);
            var bulunamayan = seriler
                .Where(s => !uretimler.Any(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase))).ToList();
            if (bulunamayan.Count > 0)
                throw new IsKuraliHatasi($"Bulunamayan seri no: {string.Join(", ", bulunamayan)}");
            var hurda = uretimler.Where(u => u.Hurda is not null).Select(u => u.SeriNo).Order().ToList();
            if (hurda.Count > 0)
                throw new IsKuraliHatasi($"Hurdaya / kayba ayrılmış ürün geri çağrılamaz: {string.Join(", ", hurda)}");
            var acik = uretimler.Where(u => u.GeriCagirmalar.Any(g => g.KapanmaZamani == null)).Select(u => u.SeriNo).Order().ToList();
            if (acik.Count > 0)
                throw new IsKuraliHatasi($"Zaten geri çağrılmış: {string.Join(", ", acik)}");

            var zaman = Zaman.Simdi();
            var kayitlar = uretimler.Select(u => new UrunGeriCagirma
            {
                Uretim = u, StokLotuId = lotId, Aciklama = aciklama, OlusturanKullanici = admin, OlusturmaZamani = zaman,
            }).ToList();
            db.UrunGeriCagirmalari.AddRange(kayitlar);
            await db.SaveChangesAsync(ct);
            var idler = kayitlar.Select(k => k.Id).ToList();
            return await Sorgu().Where(g => idler.Contains(g.Id)).OrderBy(g => g.Uretim!.SeriNo).Select(Donustur).ToListAsync(ct);
        }, ct);
    }

    public Task<GeriCagirmaDto> GeriAlAsync(int id, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Geri alma", ct);
            var g = await AcikGetirAsync(id, ct);
            var kalem = await db.SatisKalemleri.SingleOrDefaultAsync(k => k.UretimId == g.UretimId, ct)
                        ?? throw new IsKuraliHatasi("Bu ürün müşteride değil; zaten depoda.");
            var zaman = Zaman.Simdi();
            db.SatisIadeleri.Add(new SatisIade
            {
                SatisId = kalem.SatisId, UretimId = kalem.UretimId, Aciklama = $"Geri çağırma: {g.Aciklama}",
                OlusturanKullanici = admin, OlusturmaZamani = zaman,
            });
            db.SatisKalemleri.Remove(kalem);
            g.GeriAlinmaZamani = zaman;
            await db.SaveChangesAsync(ct);
            return await Sorgu().Where(x => x.Id == id).Select(Donustur).SingleAsync(ct);
        }, ct);

    public Task<GeriCagirmaDto> SatisaAcAsync(int id, int kullaniciId, CancellationToken ct = default) =>
        Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Satışa açma", ct);
            var g = await AcikGetirAsync(id, ct);
            if (await db.SatisKalemleri.AnyAsync(k => k.UretimId == g.UretimId, ct))
                throw new IsKuraliHatasi("Ürün hâlâ müşteride; önce geri alın.");
            (g.KapanmaZamani, g.KapatanKullanici) = (Zaman.Simdi(), admin);
            await db.SaveChangesAsync(ct);
            return await Sorgu().Where(x => x.Id == id).Select(Donustur).SingleAsync(ct);
        }, ct);

    public async Task<List<GeriCagirmaDto>> ListeAsync(bool acik = false, string? arama = null, CancellationToken ct = default)
    {
        var q = Sorgu();
        if (acik)
            q = q.Where(g => g.KapanmaZamani == null && g.Uretim!.Hurda == null);
        arama = arama?.Trim();
        if (!string.IsNullOrEmpty(arama))
            q = q.Where(g => EF.Functions.Like(g.Uretim!.SeriNo, Arama.Desen(arama))
                             || EF.Functions.Like(g.Uretim.Urun!.Ad, Arama.Desen(arama))
                             || (g.StokLotu != null && EF.Functions.Like(g.StokLotu.LotNo, Arama.Desen(arama)))
                             || (g.Uretim.SatisKalemi != null && EF.Functions.Like(g.Uretim.SatisKalemi.Satis!.Musteri!.Ad, Arama.Desen(arama))));
        return await q.OrderByDescending(g => g.OlusturmaZamani).ThenBy(g => g.Uretim!.SeriNo).Select(Donustur).ToListAsync(ct);
    }

    private async Task<UrunGeriCagirma> AcikGetirAsync(int id, CancellationToken ct)
    {
        var g = await db.UrunGeriCagirmalari.Include(x => x.Uretim!).ThenInclude(u => u.Hurda)
                    .SingleOrDefaultAsync(x => x.Id == id, ct)
                ?? throw new BulunamadiHatasi("Geri çağırma bulunamadı.");
        if (g.KapanmaZamani is not null)
            throw new IsKuraliHatasi("Bu geri çağırma kapanmış; ürün satışa açılmış.");
        if (g.Uretim!.Hurda is not null)
            throw new IsKuraliHatasi("Ürün hurdaya / kayba ayrılmış.");
        return g;
    }

    private IQueryable<UrunGeriCagirma> Sorgu() => db.UrunGeriCagirmalari.AsNoTracking();

    /// <summary>Durum: musteride, geri_alindi, depoda, hurda, satisa_acildi.</summary>
    private static readonly System.Linq.Expressions.Expression<Func<UrunGeriCagirma, GeriCagirmaDto>> Donustur = g =>
        new GeriCagirmaDto(
            g.Id, g.Uretim!.SeriNo, g.Uretim.Urun!.Ad, g.StokLotu == null ? null : g.StokLotu.LotNo, g.Aciklama, g.OlusturmaZamani,
            g.OlusturanKullanici!.Ad + " " + g.OlusturanKullanici.Soyad,
            g.Uretim.Hurda != null ? "hurda"
                : g.KapanmaZamani != null ? "satisa_acildi"
                : g.Uretim.SatisKalemi != null ? "musteride"
                : g.GeriAlinmaZamani != null ? "geri_alindi"
                : "depoda",
            g.Uretim.SatisKalemi != null ? g.Uretim.SatisKalemi.Satis!.Musteri!.Ad
                : g.GeriAlinmaZamani != null ? g.Uretim.Iadeler.OrderByDescending(i => i.OlusturmaZamani).Select(i => i.Satis!.Musteri!.Ad).FirstOrDefault()
                : null,
            g.Uretim.SatisKalemi != null ? g.Uretim.SatisKalemi.Satis!.Musteri!.Iletisim : null,
            g.GeriAlinmaZamani, g.KapanmaZamani);
}
