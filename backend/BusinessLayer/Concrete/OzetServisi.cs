using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class OzetServisi(AppDbContext db, IStokServisi stok) : IOzetServisi
{
    /// <summary>Parça stoğuyla bu kadar ya da daha az ürün üretilebiliyorsa uyarı verilir.</summary>
    public const int UyariEsigi = 3;
    public const int EnFazlaGun = 62;

    public async Task<AnaSayfaOzetiDto> AnaSayfaAsync(CancellationToken ct = default)
    {
        var bugun = Zaman.Bugun();
        var acikSiparisler = await db.Siparisler.AsNoTracking()
            .Where(s => s.IptalZamani == null)
            .Select(s => new
            {
                s.Id, s.No, MusteriAd = s.Musteri!.Ad, s.IstenenTarih,
                Adet = s.Kalemler.Sum(k => k.Adet),
                Gonderilen = s.Kalemler.Sum(k => k.Ayrilanlar.Count(u => u.SatisKalemi != null)),
            })
            .Where(s => s.Gonderilen < s.Adet).ToListAsync(ct);
        var bekleyen = await db.UretimEmirleri.AsNoTracking().Where(e => e.Durum == UretimEmriDurumu.Bekliyor)
            .Select(e => new { e.PlanlananTarih, Kalan = e.Adet - e.Uretilen }).ToListAsync(ct);
        var sevkiyat = await db.Satislar.AsNoTracking().Where(s => s.Durum != SevkiyatDurumu.TeslimEdildi)
            .GroupBy(s => s.Durum).Select(g => new { g.Key, Sayi = g.Count() }).ToListAsync(ct);
        var depoda = await db.Uretimler.CountAsync(u => u.SatisKalemi == null && u.Hurda == null, ct);
        var acikGeri = await db.UrunGeriCagirmalari.CountAsync(g => g.KapanmaZamani == null && g.Uretim!.Hurda == null, ct);
        var parcalar = await stok.ParcaStoklariAsync(ct: ct);

        var yaklasan = acikSiparisler.Where(s => s.IstenenTarih is { } t && t <= bugun.AddDays(7))
            .OrderBy(s => s.IstenenTarih).ThenBy(s => s.No)
            .Select(s => new YaklasanTeslimDto(s.Id, s.No, s.MusteriAd, s.IstenenTarih!.Value, s.Adet - s.Gonderilen)).ToList();

        return new AnaSayfaOzetiDto(
            acikSiparisler.Count, bekleyen.Count, bekleyen.Sum(e => e.Kalan),
            bekleyen.Where(e => e.PlanlananTarih <= bugun).Sum(e => e.Kalan),
            sevkiyat.FirstOrDefault(s => s.Key == SevkiyatDurumu.Hazirlaniyor)?.Sayi ?? 0,
            sevkiyat.FirstOrDefault(s => s.Key == SevkiyatDurumu.Kargoda)?.Sayi ?? 0,
            depoda, parcalar.Count(p => p.MinAltinda), acikGeri,
            await UyarilarAsync(parcalar, ct), yaklasan);
    }

    public async Task<List<TakvimGunuDto>> TakvimAsync(DateOnly baslangic, DateOnly bitis, CancellationToken ct = default)
    {
        if (bitis < baslangic)
            throw new IsKuraliHatasi("Bitiş başlangıçtan önce olamaz.");
        if (bitis.DayNumber - baslangic.DayNumber >= EnFazlaGun)
            throw new IsKuraliHatasi($"Takvim en çok {EnFazlaGun} gün gösterir.");

        var planlanan = await db.UretimEmirleri.AsNoTracking()
            .Where(e => e.Durum == UretimEmriDurumu.Bekliyor && e.PlanlananTarih >= baslangic && e.PlanlananTarih <= bitis)
            .Select(e => new { Tarih = e.PlanlananTarih, e.UrunId, Adet = e.Adet - e.Uretilen }).ToListAsync(ct);
        // Üretim anı İstanbul saatine göre güne yerleşir; sınırlarda bir gün pay bırakılır
        var ilk = new DateTimeOffset(baslangic.AddDays(-1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var son = new DateTimeOffset(bitis.AddDays(2).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var uretilen = (await db.Uretimler.AsNoTracking().Where(u => u.Tarih >= ilk && u.Tarih < son)
                .Select(u => new { u.Tarih, u.UrunId }).ToListAsync(ct))
            .Select(u => new { Tarih = Zaman.YerelTarih(u.Tarih), u.UrunId })
            .Where(u => u.Tarih >= baslangic && u.Tarih <= bitis).ToList();
        var istenen = await db.SiparisKalemleri.AsNoTracking()
            .Where(k => k.Siparis!.IptalZamani == null)
            .Select(k => new { Tarih = k.Siparis!.IstenenTarih ?? k.Siparis.Tarih, k.UrunId, k.Adet })
            .Where(k => k.Tarih >= baslangic && k.Tarih <= bitis).ToListAsync(ct);

        var urunAdlari = await db.Urunler.AsNoTracking().ToDictionaryAsync(u => u.Id, u => u.Ad, ct);
        var satirlar = planlanan.Select(p => (p.Tarih, p.UrunId, Plan: p.Adet, Uret: 0, Ist: 0))
            .Concat(uretilen.Select(u => (u.Tarih, u.UrunId, Plan: 0, Uret: 1, Ist: 0)))
            .Concat(istenen.Select(i => (i.Tarih, i.UrunId, Plan: 0, Uret: 0, Ist: i.Adet)));
        return [.. satirlar.GroupBy(s => s.Tarih).OrderBy(g => g.Key)
            .Select(g => new TakvimGunuDto(g.Key, [.. g.GroupBy(s => s.UrunId)
                .Select(u => new TakvimUrunuDto(u.Key, urunAdlari.GetValueOrDefault(u.Key, "?"),
                    u.Sum(x => x.Plan), u.Sum(x => x.Uret), u.Sum(x => x.Ist)))
                .OrderBy(u => u.UrunAd, StringComparer.CurrentCulture)]))];
    }

    /// <summary>Ürün ağacına göre serbest stokla üretilebilecek adet; eşik ve altındakiler, en az olan önce.</summary>
    private async Task<List<UretilebilirUyariDto>> UyarilarAsync(List<ParcaStokDto> parcalar, CancellationToken ct)
    {
        var serbest = parcalar.ToDictionary(p => p.Id);
        var urunler = await db.Urunler.AsNoTracking().Include(u => u.Agac).ThenInclude(a => a.Parca)
            .Where(u => u.Agac.Any()).ToListAsync(ct);
        return [.. urunler
            .Select(u =>
            {
                var satirlar = u.Agac.Select(a => (Agac: a, Serbest: serbest.TryGetValue(a.ParcaId, out var p) ? p.Serbest : 0))
                    .Select(x => (x.Agac, x.Serbest, Adet: x.Serbest / x.Agac.Adet)).ToList();
                var enAz = satirlar.Min(x => x.Adet);
                return new UretilebilirUyariDto(u.Id, u.Kod, u.Ad, enAz,
                    [.. satirlar.Where(x => x.Adet <= UyariEsigi).OrderBy(x => x.Adet).ThenBy(x => x.Agac.Parca!.Kod)
                        .Select(x => new UretilebilirParcaDto(Donusum.Ozet(x.Agac.Parca!), x.Agac.Adet, x.Serbest))]);
            })
            .Where(u => u.Uretilebilir <= UyariEsigi)
            .OrderBy(u => u.Uretilebilir).ThenBy(u => u.UrunAd, StringComparer.CurrentCulture)];
    }
}
