using LotTakip.DataAccess;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

public interface ISatisServisi
{
    /// <summary>Satış listesine eklenecek seri no'yu doğrular: var mı, daha önce satıldı mı. Değilse SatisHatasi.</summary>
    Task<SatilabilirDto> SatilabilirAsync(string seriNo, CancellationToken ct = default);

    /// <summary>Geri çağrılan lot içeren seri numaraları ve ilgili lotlar (kaydetmeden kontrol).</summary>
    Task<List<SatisUyarisiDto>> SatisUyarilariAsync(IReadOnlyCollection<string> seriNolar, CancellationToken ct = default);

    /// <summary>
    /// Seri numaralarını müşteriye satar (müşteri yoksa oluşturur). Bulunamayan, tekrarlanan veya
    /// daha önce satılmış seri no varsa hiçbir şey kaydedilmez (SatisHatasi). Geri çağrılan lot
    /// içeren ürün varsa ve onay verilmediyse GeriCagrilanLotOnayiGerekliHatasi.
    /// </summary>
    Task<SatisSonucuDto> SatAsync(SatisIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Satış geçmişi: her satış + ürün için bir satır (yeniden eskiye). Arama müşteri adı veya seri no'da.</summary>
    Task<List<SatisGecmisiDto>> SatisGecmisiAsync(string? arama = null, int adet = 50, CancellationToken ct = default);
}

public class SatisServisi(AppDbContext db) : ISatisServisi
{
    public async Task<SatilabilirDto> SatilabilirAsync(string seriNo, CancellationToken ct = default)
    {
        var no = (seriNo ?? "").Trim();
        var uretim = await db.Uretimler.AsNoTracking().Include(u => u.Urun)
                         .SingleOrDefaultAsync(u => u.SeriNo == no, ct)
                     ?? throw new SatisHatasi($"{no} seri numarası bulunamadı.");
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
            .Where(t => liste.Contains(t.Uretim!.SeriNo) && t.StokLotu!.GeriCagrildi)
            .Select(t => new { t.Uretim!.SeriNo, t.StokLotu!.LotNo })
            .Distinct().ToListAsync(ct);
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
            throw new SatisHatasi("En az bir seri no girilmeli.");
        var tekrarlar = seriler.GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1).Select(g => g.Key).Order().ToList();
        if (tekrarlar.Count > 0)
            throw new SatisHatasi($"Listede tekrarlanan seri no: {string.Join(", ", tekrarlar)}");

        return Islem.CalistirAsync(db, async () =>
        {
            var kullanici = await Kullanicilar.GetirAsync(db, kullaniciId, ct);
            var uretimler = await db.Uretimler.Where(u => seriler.Contains(u.SeriNo)).ToListAsync(ct);
            var bulunamayan = seriler
                .Where(s => !uretimler.Any(u => string.Equals(u.SeriNo, s, StringComparison.OrdinalIgnoreCase)))
                .ToList();
            if (bulunamayan.Count > 0)
                throw new SatisHatasi($"Bulunamayan seri no: {string.Join(", ", bulunamayan)}");

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
            var satis = new Satis
            {
                Musteri = musteri, Tarih = istek.Tarih ?? Zaman.Bugun(),
                OlusturanKullanici = kullanici, OlusturmaZamani = Zaman.Simdi(),
            };
            satis.Kalemler = [.. sirali.Select(u => new SatisKalemi { Satis = satis, Uretim = u })];
            db.Satislar.Add(satis);
            await db.SaveChangesAsync(ct);

            return new SatisSonucuDto(satis.Id, musteri.Id, musteri.Ad, yeniMusteri, satis.Tarih,
                [.. sirali.Select(u => u.SeriNo)], uyarilar);
        }, ct);
    }

    public async Task<List<SatisGecmisiDto>> SatisGecmisiAsync(string? arama = null, int adet = 50,
                                                               CancellationToken ct = default)
    {
        var q = db.Satislar.AsNoTracking();
        arama = arama?.Trim();
        if (!string.IsNullOrEmpty(arama))
            q = q.Where(s => s.Musteri!.Ad.Contains(arama) || s.Kalemler.Any(k => k.Uretim!.SeriNo.Contains(arama)));
        var satislar = await q.OrderByDescending(s => s.Tarih).ThenByDescending(s => s.Id).Take(adet)
            .Select(s => new
            {
                s.Id, s.Tarih, MusteriAd = s.Musteri!.Ad,
                Kalemler = s.Kalemler.Select(k => new { k.Uretim!.SeriNo, UrunAd = k.Uretim.Urun!.Ad }).ToList(),
            }).ToListAsync(ct);
        return [.. satislar.SelectMany(s => s.Kalemler
            .GroupBy(k => k.UrunAd).OrderBy(g => g.Key, StringComparer.CurrentCulture)
            .Select(g => new SatisGecmisiDto(s.Id, s.Tarih, s.MusteriAd, g.Key, [.. g.Select(k => k.SeriNo).Order()])))];
    }

    // --- iç ------------------------------------------------------------------

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
