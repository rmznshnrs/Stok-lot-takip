using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class SatisServisi(AppDbContext db) : ISatisServisi
{
    public async Task<SatilabilirDto> SatilabilirAsync(string seriNo, CancellationToken ct = default)
    {
        var no = (seriNo ?? "").Trim();
        var uretim = await db.Uretimler.AsNoTracking().Include(u => u.Urun).Include(u => u.Hurda)
                         .SingleOrDefaultAsync(u => u.SeriNo == no, ct)
                     ?? throw new SatisHatasi($"{no} seri numarası bulunamadı.");
        if (uretim.Hurda is not null)
            throw new SatisHatasi(HurdaMesaji([uretim]));
        if (await db.UrunGeriCagirmalari.AnyAsync(g => g.UretimId == uretim.Id && g.KapanmaZamani == null, ct))
            throw new SatisHatasi(GeriCagirmaMesaji([uretim.SeriNo]));
        var satis = await SatildigiSatisAsync([uretim.Id], ct);
        if (satis.Count > 0)
            throw new SatisHatasi(SatilmisMesaji(satis));
        var uyari = await SatisUyarilariAsync([uretim.SeriNo], ct);
        return new SatilabilirDto(uretim.Id, uretim.SeriNo, uretim.Urun!.Ad, uyari.FirstOrDefault()?.Lotlar ?? []);
    }

    public async Task<List<SatisUyarisiDto>> SatisUyarilariAsync(IReadOnlyCollection<string> seriNolar,
                                                                 CancellationToken ct = default)
    {
        var liste = seriNolar.Select(s => s.Trim()).ToList();
        var satirlar = await db.UretimTuketimleri.AsNoTracking()
            .Where(t => liste.Contains(t.Uretim!.SeriNo) && t.StokLotu!.Sorunlu)
            .Select(t => new { t.Uretim!.SeriNo, t.StokLotu!.LotNo })
            .Distinct().ToListAsync(ct);
        // Hatalı işaretlenmiş ürün lotundaki seri numaraları da uyarı verir
        satirlar.AddRange(await db.Uretimler.AsNoTracking()
            .Where(u => liste.Contains(u.SeriNo) && db.UrunLotSorunlari.Any(s => s.LotNo == u.LotNo))
            .Select(u => new { u.SeriNo, u.LotNo }).ToListAsync(ct));
        var lotlar = satirlar.GroupBy(s => s.SeriNo, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(s => s.LotNo).Order().ToList(), StringComparer.OrdinalIgnoreCase);
        // Verilen sırayla; veritabanındaki yazımla
        return [.. satirlar.Select(s => s.SeriNo).Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(s => liste.FindIndex(x => string.Equals(x, s, StringComparison.OrdinalIgnoreCase)))
            .Select(s => new SatisUyarisiDto(s, lotlar[s]))];
    }

    public Task<SatisSonucuDto> SatAsync(SatisIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var musteriAdi = Donusum.Sadelestir(istek.MusteriAdi);
        if (musteriAdi.Length == 0)
            throw new SatisHatasi("Müşteri adı boş olamaz.");
        var seriler = (istek.SeriNolar ?? []).Select(s => (s ?? "").Trim()).Where(s => s.Length > 0).ToList();
        if (seriler.Count == 0)
            throw new SatisHatasi("Seri no girin.");
        var tekrarlar = seriler.GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).Order().ToList();
        if (tekrarlar.Count > 0)
            throw new SatisHatasi($"Listede tekrarlanan seri no: {string.Join(", ", tekrarlar)}");

        return Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var uretimler = await db.Uretimler.Include(u => u.Hurda).Where(u => seriler.Contains(u.SeriNo)).ToListAsync(ct);
            var bulunamayan = seriler
                .Where(s => !uretimler.Any(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (bulunamayan.Count > 0)
                throw new SatisHatasi($"Bulunamayan seri no: {string.Join(", ", bulunamayan)}");

            var hurdalar = uretimler.Where(u => u.Hurda is not null).ToList();
            if (hurdalar.Count > 0)
                throw new SatisHatasi(HurdaMesaji(hurdalar));
            var uretimIdleri = uretimler.Select(u => u.Id).ToList();
            var geriCagrilan = await db.UrunGeriCagirmalari
                .Where(g => uretimIdleri.Contains(g.UretimId) && g.KapanmaZamani == null)
                .Select(g => g.Uretim!.SeriNo).ToListAsync(ct);
            if (geriCagrilan.Count > 0)
                throw new SatisHatasi(GeriCagirmaMesaji(geriCagrilan));

            // Açık kontrol: bire bir ilişkide EF eski kalemi sessizce silebilir
            var satilmis = await SatildigiSatisAsync([.. uretimler.Select(u => u.Id)], ct);
            if (satilmis.Count > 0)
                throw new SatisHatasi(SatilmisMesaji(satilmis));

            var uyarilar = await SatisUyarilariAsync([.. uretimler.Select(u => u.SeriNo)], ct);
            if (uyarilar.Count > 0 && !istek.GeriCagrilanOnayi)
                throw new GeriCagrilanLotOnayiGerekliHatasi(uyarilar);

            var musteri = await db.Musteriler.SingleOrDefaultAsync(m => m.Ad == musteriAdi, ct);
            var yeniMusteri = musteri is null;
            musteri ??= new Musteri { Ad = musteriAdi };

            // Kalemler verilen sırayla
            var sirali = seriler.Select(s => uretimler.First(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase))).ToList();
            // Saat verilmişse (ya da hiç tarih verilmemişse) satış saatiyle kaydedilir; yalnız tarih verilirse saat boş kalır
            var an = istek.Zaman ?? (istek.Tarih is null ? Zaman.Simdi() : null);
            var satis = new Satis
            {
                Musteri = musteri, Tarih = an is { } z ? Zaman.YerelTarih(z) : istek.Tarih!.Value, Zaman = an,
                OlusturanKullanici = kullanici, OlusturmaZamani = Zaman.Simdi(),
            };
            satis.Kalemler = [.. sirali.Select(u => new SatisKalemi { Satis = satis, Uretim = u })];
            db.Satislar.Add(satis);
            await db.SaveChangesAsync(ct);

            return new SatisSonucuDto(satis.Id, musteri.Id, musteri.Ad, yeniMusteri, satis.Tarih,
                [.. sirali.Select(u => u.SeriNo)], uyarilar);
        }, ct);
    }

    public async Task<List<SatisGecmisiDto>> SatisGecmisiAsync(string? arama = null, int adet = 500,
                                                               CancellationToken ct = default)
    {
        var q = db.Satislar.AsNoTracking();
        arama = arama?.Trim();
        if (!string.IsNullOrEmpty(arama))
            q = q.Where(s => EF.Functions.Like(s.Musteri!.Ad, Arama.Desen(arama))
                             || s.Kalemler.Any(k => EF.Functions.Like(k.Uretim!.SeriNo, Arama.Desen(arama)))
                             || s.Iadeler.Any(i => EF.Functions.Like(i.Uretim!.SeriNo, Arama.Desen(arama))));
        var satislar = await q.OrderByDescending(s => s.Tarih).ThenByDescending(s => s.Zaman).ThenByDescending(s => s.Id).Take(adet)
            .Select(s => new
            {
                s.Id, s.Tarih, s.Zaman, MusteriAd = s.Musteri!.Ad,
                Satilan = s.Kalemler.Select(k => new { k.Uretim!.SeriNo, k.Uretim.LotNo, UrunAd = k.Uretim.Urun!.Ad }).ToList(),
                Iade = s.Iadeler.Select(i => new { i.Uretim!.SeriNo, UrunAd = i.Uretim.Urun!.Ad }).ToList(),
            }).ToListAsync(ct);
        return [.. satislar.SelectMany(s => s.Satilan.Select(k => (k.SeriNo, k.LotNo, k.UrunAd, Iade: false))
            .Concat(s.Iade.Select(i => (i.SeriNo, LotNo: "", i.UrunAd, Iade: true)))
            .GroupBy(k => k.UrunAd).OrderBy(g => g.Key, StringComparer.CurrentCulture)
            .Select(g => new SatisGecmisiDto(s.Id, s.Tarih, s.MusteriAd, g.Key,
                [.. g.Where(k => !k.Iade).Select(k => k.SeriNo).Order()],
                [.. g.Where(k => k.Iade).Select(k => k.SeriNo).Distinct().Order()],
                [.. g.Where(k => !k.Iade).OrderBy(k => k.SeriNo).Select(k => new SatisKalemiDto(k.SeriNo, k.LotNo))], s.Zaman)))];
    }

    public Task<SatisIadeSonucuDto> IadeAlAsync(int satisId, SatisIadeIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var aciklama = (istek.Aciklama ?? "").Trim();
        if (aciklama.Length == 0)
            throw new IsKuraliHatasi("Açıklama girin.");
        var seriler = (istek.SeriNolar ?? []).Select(s => (s ?? "").Trim()).Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (seriler.Count == 0)
            throw new IsKuraliHatasi("Seri no seçin.");

        return Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "İade alma", ct);
            if (!await db.Satislar.AnyAsync(s => s.Id == satisId, ct))
                throw new BulunamadiHatasi("Satış bulunamadı.");
            var kalemler = await db.SatisKalemleri.Include(k => k.Uretim)
                .Where(k => k.SatisId == satisId && seriler.Contains(k.Uretim!.SeriNo)).ToListAsync(ct);
            var olmayan = seriler
                .Where(s => !kalemler.Any(k => string.Equals(k.Uretim!.SeriNo, s, StringComparison.OrdinalIgnoreCase))).ToList();
            if (olmayan.Count > 0)
                throw new IsKuraliHatasi($"Bu satışta yok: {string.Join(", ", olmayan)}");

            var zaman = Zaman.Simdi();
            foreach (var k in kalemler)
            {
                db.SatisIadeleri.Add(new SatisIade
                {
                    SatisId = satisId, UretimId = k.UretimId, Aciklama = aciklama, OlusturanKullanici = admin, OlusturmaZamani = zaman,
                });
            }
            db.SatisKalemleri.RemoveRange(kalemler);
            await db.SaveChangesAsync(ct);
            return new SatisIadeSonucuDto(satisId, [.. kalemler.Select(k => k.Uretim!.SeriNo).Order()]);
        }, ct);
    }

    // --- iç ------------------------------------------------------------------

    private static string GeriCagirmaMesaji(IEnumerable<string> seriNolar) =>
        $"Geri çağrılmış ürün satılamaz: {string.Join(", ", seriNolar.Order())}";

    private static string HurdaMesaji(IEnumerable<Uretim> uretimler) =>
        $"Hurdaya / kayba ayrılmış ürün satılamaz: {string.Join(", ", uretimler.Select(u => u.SeriNo).Order())}";

    private sealed record SatilmisKayit(string SeriNo, string MusteriAd, DateOnly Tarih);

    private async Task<List<SatilmisKayit>> SatildigiSatisAsync(List<int> uretimIdleri, CancellationToken ct) =>
        await db.SatisKalemleri.AsNoTracking()
            .Where(k => uretimIdleri.Contains(k.UretimId))
            .OrderBy(k => k.Uretim!.SeriNo)
            .Select(k => new SatilmisKayit(k.Uretim!.SeriNo, k.Satis!.Musteri!.Ad, k.Satis.Tarih))
            .ToListAsync(ct);

    private static string SatilmisMesaji(List<SatilmisKayit> kayitlar) =>
        kayitlar.Count == 1
            ? $"{kayitlar[0].SeriNo} daha önce satılmış: {kayitlar[0].MusteriAd}, {kayitlar[0].Tarih:dd.MM.yyyy}."
            : $"Daha önce satılmış seri no: {string.Join(", ", kayitlar.Select(k => k.SeriNo))}";
}
