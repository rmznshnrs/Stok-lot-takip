using LotTakip.DataAccess;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Business;

public interface IIzlemeServisi
{
    /// <summary>Parçadan bütüne: lotun kullanıldığı seri numaraları ve satıldığı müşteriler. Yoksa null.</summary>
    Task<LotIzDto?> LotIzleAsync(string lotNo, CancellationToken ct = default);

    /// <summary>Bütünden parçaya: seri no'lu üründeki parçalar, lotları, adetleri ve satış bilgisi. Yoksa null.</summary>
    Task<SeriIzDto?> SeriIzleAsync(string seriNo, CancellationToken ct = default);

    /// <summary>
    /// Girilen numaranın türünü algılar: birebir lot no, seri no veya parça kodu; yoksa numarayı
    /// içeren lot ve seri numaraları aday olarak döner (tek aday varsa doğrudan o seçilir).
    /// </summary>
    Task<NumaraAramaDto> NumaraAraAsync(string metin, CancellationToken ct = default);

    /// <summary>Son mal girişi, üretim ve satışlar (yeniden eskiye). Aynı anda yapılan üretimler tek satır.</summary>
    Task<List<HareketDto>> SonHareketlerAsync(int adet = 8, CancellationToken ct = default);

    /// <summary>Lotu geri çağırır veya geri çağırmayı kaldırır (yalnız Admin).</summary>
    Task<LotDto> LotGeriCagirAsync(string lotNo, bool geriCagir, int kullaniciId, CancellationToken ct = default);
}

public class IzlemeServisi(AppDbContext db) : IIzlemeServisi
{
    public async Task<LotIzDto?> LotIzleAsync(string lotNo, CancellationToken ct = default)
    {
        var no = (lotNo ?? "").Trim();
        var lot = await db.StokLotlari.AsNoTracking().Include(l => l.Parca).Include(l => l.Tedarikci)
            .SingleOrDefaultAsync(l => l.LotNo == no, ct);
        if (lot is null)
            return null;

        var satirlar = await db.UretimTuketimleri.AsNoTracking()
            .Where(t => t.StokLotuId == lot.Id)
            .GroupBy(t => new
            {
                t.UretimId, t.Uretim!.SeriNo, t.Uretim.Tarih, UrunKod = t.Uretim.Urun!.Kod, UrunAd = t.Uretim.Urun.Ad,
                SatisId = (int?)t.Uretim.SatisKalemi!.SatisId, SatisTarih = (DateOnly?)t.Uretim.SatisKalemi.Satis!.Tarih,
                MusteriId = (int?)t.Uretim.SatisKalemi.Satis.MusteriId, MusteriAd = t.Uretim.SatisKalemi.Satis.Musteri!.Ad,
                MusteriIletisim = t.Uretim.SatisKalemi.Satis.Musteri.Iletisim,
            })
            .Select(g => new { g.Key, Adet = g.Sum(t => t.Adet) })
            .ToListAsync(ct);

        var uretimler = satirlar.OrderBy(s => s.Key.SeriNo).Select(s => new LotIzUretimDto(
            s.Key.UretimId, s.Key.SeriNo, s.Key.Tarih, s.Key.UrunKod, s.Key.UrunAd, s.Adet,
            s.Key.SatisId is null ? null : new SatisBilgisiDto(
                s.Key.SatisId.Value, s.Key.SatisTarih!.Value, s.Key.MusteriId!.Value, s.Key.MusteriAd!, s.Key.MusteriIletisim!)))
            .ToList();

        var musteriler = uretimler.Where(u => u.Satis is not null)
            .GroupBy(u => u.Satis!.MusteriId)
            .Select(g => new LotIzMusteriDto(g.Key, g.First().Satis!.MusteriAd, g.First().Satis!.MusteriIletisim,
                [.. g.Select(u => new MusteriSatisiDto(u.SeriNo, u.Satis!.Tarih))]))
            .OrderBy(m => m.Ad, StringComparer.CurrentCulture)
            .ToList();

        return new LotIzDto(Donusum.Lot(lot), uretimler.Sum(u => u.Adet), uretimler, musteriler);
    }

    public async Task<SeriIzDto?> SeriIzleAsync(string seriNo, CancellationToken ct = default)
    {
        var no = (seriNo ?? "").Trim();
        var uretim = await db.Uretimler.AsNoTracking()
            .Include(u => u.Urun)
            .Include(u => u.Tuketimler).ThenInclude(t => t.StokLotu!).ThenInclude(l => l.Parca)
            .Include(u => u.Tuketimler).ThenInclude(t => t.StokLotu!).ThenInclude(l => l.Tedarikci)
            .Include(u => u.SatisKalemi!).ThenInclude(k => k.Satis!).ThenInclude(s => s.Musteri)
            .AsSplitQuery()
            .SingleOrDefaultAsync(u => u.SeriNo == no, ct);
        if (uretim is null)
            return null;

        var parcalar = uretim.Tuketimler
            .OrderBy(t => t.StokLotu!.Parca!.Kod).ThenBy(t => t.StokLotu!.SiparisTarihi).ThenBy(t => t.StokLotuId)
            .Select(t => new SeriIzParcaDto(Donusum.Ozet(t.StokLotu!.Parca!), Donusum.Lot(t.StokLotu!), t.Adet))
            .ToList();
        var satis = uretim.SatisKalemi?.Satis;
        return new SeriIzDto(uretim.Id, uretim.SeriNo, uretim.Tarih, uretim.Urun!.Kod, uretim.Urun.Ad, parcalar,
            satis is null ? null : new SatisBilgisiDto(satis.Id, satis.Tarih, satis.MusteriId, satis.Musteri!.Ad, satis.Musteri.Iletisim));
    }

    public async Task<NumaraAramaDto> NumaraAraAsync(string metin, CancellationToken ct = default)
    {
        var m = (metin ?? "").Trim();
        if (m.Length == 0)
            return new(null, null, []);

        if (await db.StokLotlari.Where(l => l.LotNo == m).Select(l => l.LotNo).FirstOrDefaultAsync(ct) is { } lot)
            return new("lot", lot, []);
        if (await db.Uretimler.Where(u => u.SeriNo == m).Select(u => u.SeriNo).FirstOrDefaultAsync(ct) is { } seri)
            return new("seri", seri, []);
        if (await db.Parcalar.Where(p => p.Kod == m).Select(p => p.Kod).FirstOrDefaultAsync(ct) is { } kod)
            return new("parca", kod, []);

        var adaylar = (await db.StokLotlari.Where(l => l.LotNo.Contains(m)).OrderBy(l => l.LotNo)
                .Select(l => l.LotNo).Take(10).ToListAsync(ct)).Select(x => new AdayDto("lot", x))
            .Concat((await db.Uretimler.Where(u => u.SeriNo.Contains(m)).OrderBy(u => u.SeriNo)
                .Select(u => u.SeriNo).Take(10).ToListAsync(ct)).Select(x => new AdayDto("seri", x)))
            .ToList();
        return adaylar.Count == 1 ? new(adaylar[0].Tur, adaylar[0].Deger, []) : new(null, null, adaylar);
    }

    public async Task<List<HareketDto>> SonHareketlerAsync(int adet = 8, CancellationToken ct = default)
    {
        // (tarih, tür önceliği, sıra) ile yeniden eskiye; aynı gün satış > üretim > giriş
        var hareketler = new List<(HareketDto Hareket, int Oncelik, long Sira)>();

        var lotlar = await db.StokLotlari.AsNoTracking().Include(l => l.Parca).Include(l => l.Tedarikci)
            .OrderByDescending(l => l.GirisZamani).ThenByDescending(l => l.Id).Take(adet).ToListAsync(ct);
        hareketler.AddRange(lotlar.Select(l => (new HareketDto(
            "giris", Zaman.YerelTarih(l.GirisZamani),
            $"{l.Parca!.Ad} · {l.GirisAdet} {l.Parca.Birim} · {l.Tedarikci!.Ad}", l.LotNo, l.LotNo, "lot"),
            0, l.GirisZamani.UtcTicks)));

        // Tek seferde yapılan üretimler aynı Tarih'i taşır → (ürün, tarih) grubu tek satır
        var uretimler = await db.Uretimler.AsNoTracking()
            .OrderByDescending(u => u.Tarih).ThenByDescending(u => u.Id).Take(adet * 20)
            .Select(u => new { u.Id, u.UrunId, UrunAd = u.Urun!.Ad, u.Tarih, u.SeriNo }).ToListAsync(ct);
        foreach (var g in uretimler.GroupBy(u => (u.UrunId, u.Tarih)).Take(adet))
        {
            var seriler = g.Select(u => u.SeriNo).Order().ToList();
            hareketler.Add((new HareketDto("uretim", Zaman.YerelTarih(g.Key.Tarih), $"{g.First().UrunAd} · {seriler.Count} adet",
                Aralik(seriler), seriler[0], "seri"), 1, g.Max(u => u.Id)));
        }

        var satislar = await db.Satislar.AsNoTracking()
            .OrderByDescending(s => s.Tarih).ThenByDescending(s => s.Id).Take(adet)
            .Select(s => new
            {
                s.Id, s.Tarih, MusteriAd = s.Musteri!.Ad,
                Kalemler = s.Kalemler.Select(k => new { k.Uretim!.SeriNo, UrunAd = k.Uretim.Urun!.Ad }).ToList(),
            }).ToListAsync(ct);
        foreach (var s in satislar.Where(s => s.Kalemler.Count > 0))
        {
            var seriler = s.Kalemler.Select(k => k.SeriNo).Order().ToList();
            var urunler = string.Join(", ", s.Kalemler.Select(k => k.UrunAd).Distinct().Order());
            var adetMetni = seriler.Count > 1 ? $" · {seriler.Count} adet" : "";
            hareketler.Add((new HareketDto("satis", s.Tarih, $"{urunler} · {s.MusteriAd}{adetMetni}",
                Aralik(seriler), seriler[0], "seri"), 2, s.Id));
        }

        return [.. hareketler
            .OrderByDescending(h => h.Hareket.Tarih).ThenByDescending(h => h.Oncelik).ThenByDescending(h => h.Sira)
            .Take(adet).Select(h => h.Hareket)];
    }

    private static string Aralik(List<string> seriler) =>
        seriler.Count == 1 ? seriler[0] : $"{seriler[0]} … {seriler[^1]}";

    public Task<LotDto> LotGeriCagirAsync(string lotNo, bool geriCagir, int kullaniciId, CancellationToken ct = default)
    {
        var no = (lotNo ?? "").Trim();
        return Islem.CalistirAsync(db, async () =>
        {
            await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Lot geri çağırma", ct);
            var lot = await db.StokLotlari.Include(l => l.Parca).Include(l => l.Tedarikci)
                          .SingleOrDefaultAsync(l => l.LotNo == no, ct)
                      ?? throw new BulunamadiHatasi($"{no} lotu bulunamadı.");
            if (lot.GeriCagrildi != geriCagir)
            {
                lot.GeriCagrildi = geriCagir;
                await db.SaveChangesAsync(ct);
            }
            return Donusum.Lot(lot);
        }, ct);
    }
}
