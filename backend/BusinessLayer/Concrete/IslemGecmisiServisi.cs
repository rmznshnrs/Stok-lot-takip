using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class IslemGecmisiServisi(AppDbContext db) : IIslemGecmisiServisi
{
    public static readonly string[] Turler = ["giris", "uretim", "satis", "iade", "duzeltme", "hurda", "sorun", "geri"];

    public async Task<List<IslemDto>> IslemlerAsync(int kullaniciId, string? tur = null, int? yapanId = null,
                                                    DateTimeOffset? once = null, int adet = 50, CancellationToken ct = default)
    {
        await Kullanicilar.AdminGetirAsync(db, kullaniciId, "İşlem geçmişi", ct);
        if (tur is not null && !Turler.Contains(tur))
            throw new IsKuraliHatasi($"Tür şunlardan biri olmalı: {string.Join(", ", Turler)}.");
        adet = Math.Clamp(adet, 1, 200);
        var sinir = once ?? DateTimeOffset.MaxValue;
        bool Al(string t) => tur is null || tur == t;
        var liste = new List<IslemDto>();

        if (Al("giris"))
        {
            liste.AddRange(await db.StokLotlari.AsNoTracking()
                .Where(l => l.OlusturmaZamani < sinir && (yapanId == null || l.OlusturanKullaniciId == yapanId))
                .OrderByDescending(l => l.OlusturmaZamani).Take(adet)
                .Select(l => new IslemDto(l.OlusturmaZamani, "giris", l.OlusturanKullanici!.Ad + " " + l.OlusturanKullanici.Soyad,
                    l.Parca!.Ad + " · " + l.GirisAdet + " " + l.Parca.Birim + " · " + l.Tedarikci!.Ad, l.LotNo, "lot"))
                .ToListAsync(ct));
        }

        if (Al("uretim"))
        {
            var gruplar = await db.Uretimler.AsNoTracking()
                .Where(u => u.OlusturmaZamani < sinir && (yapanId == null || u.OlusturanKullaniciId == yapanId))
                // Bir seferde üretilenler aynı Tarih'i taşır (kayıt zamanları milisaniye farklı olabilir)
                .GroupBy(u => new { u.Tarih, u.OlusturanKullaniciId, UrunAd = u.Urun!.Ad,
                                    Kisi = u.OlusturanKullanici!.Ad + " " + u.OlusturanKullanici.Soyad })
                .Select(g => new { g.Key, Zaman = g.Max(u => u.OlusturmaZamani), Adet = g.Count(),
                                   Ilk = g.Min(u => u.SeriNo), Son = g.Max(u => u.SeriNo) })
                .OrderByDescending(g => g.Zaman).Take(adet)
                .ToListAsync(ct);
            liste.AddRange(gruplar.Select(g => new IslemDto(g.Zaman, "uretim", g.Key.Kisi,
                $"{g.Key.UrunAd} · {g.Adet} adet{(g.Adet > 1 ? $" · {g.Ilk} … {g.Son}" : "")}", g.Ilk, "seri")));
        }

        if (Al("satis"))
        {
            var satislar = await db.Satislar.AsNoTracking()
                .Where(s => s.OlusturmaZamani < sinir && (yapanId == null || s.OlusturanKullaniciId == yapanId))
                .OrderByDescending(s => s.OlusturmaZamani).Take(adet)
                .Select(s => new
                {
                    s.OlusturmaZamani, MusteriAd = s.Musteri!.Ad, Kisi = s.OlusturanKullanici!.Ad + " " + s.OlusturanKullanici.Soyad,
                    Adet = s.Kalemler.Count + s.Iadeler.Count,
                    Ilk = s.Kalemler.Min(k => k.Uretim!.SeriNo) ?? s.Iadeler.Min(i => i.Uretim!.SeriNo),
                })
                .ToListAsync(ct);
            liste.AddRange(satislar.Select(s => new IslemDto(s.OlusturmaZamani, "satis", s.Kisi,
                $"{s.MusteriAd} · {s.Adet} ürün", s.Ilk, s.Ilk is null ? null : "seri")));
        }

        if (Al("iade"))
        {
            var gruplar = await db.SatisIadeleri.AsNoTracking()
                .Where(i => i.OlusturmaZamani < sinir && (yapanId == null || i.OlusturanKullaniciId == yapanId))
                .GroupBy(i => new { i.OlusturmaZamani, i.SatisId, i.Aciklama, MusteriAd = i.Satis!.Musteri!.Ad,
                                    Kisi = i.OlusturanKullanici!.Ad + " " + i.OlusturanKullanici.Soyad })
                .OrderByDescending(g => g.Key.OlusturmaZamani).Take(adet)
                .Select(g => new { g.Key, Adet = g.Count(), Ilk = g.Min(i => i.Uretim!.SeriNo) })
                .ToListAsync(ct);
            liste.AddRange(gruplar.Select(g => new IslemDto(g.Key.OlusturmaZamani, "iade", g.Key.Kisi,
                $"{g.Key.MusteriAd} · {g.Adet} ürün · {g.Key.Aciklama}", g.Ilk, "seri")));
        }

        if (Al("duzeltme"))
        {
            var duzeltmeler = await db.StokDuzeltmeleri.AsNoTracking()
                .Where(d => d.OlusturmaZamani < sinir && (yapanId == null || d.OlusturanKullaniciId == yapanId))
                .OrderByDescending(d => d.OlusturmaZamani).Take(adet)
                .Select(d => new
                {
                    d.OlusturmaZamani, d.Miktar, d.HataliAdet, d.Tur, d.Aciklama, d.StokLotu!.LotNo, ParcaAd = d.StokLotu.Parca!.Ad,
                    Kisi = d.OlusturanKullanici!.Ad + " " + d.OlusturanKullanici.Soyad,
                })
                .ToListAsync(ct);
            liste.AddRange(duzeltmeler.Select(d => new IslemDto(d.OlusturmaZamani, "duzeltme", d.Kisi,
                d.Tur == StokDuzeltmeTuru.Hatali
                    ? $"{d.ParcaAd} · {d.HataliAdet} hatalı · {(d.Miktar < 0 ? "stoktan düşüldü" : "stokta bırakıldı")} · {d.Aciklama}"
                    : $"{d.ParcaAd} · {(d.Miktar > 0 ? "+" : "")}{d.Miktar} · {DuzeltmeAdi(d.Tur.ToString())} · {d.Aciklama}",
                d.LotNo, "lot")));
        }

        if (Al("hurda"))
        {
            var gruplar = await db.UretimHurdalari.AsNoTracking()
                .Where(h => h.OlusturmaZamani < sinir && (yapanId == null || h.OlusturanKullaniciId == yapanId))
                .GroupBy(h => new { h.OlusturmaZamani, h.Tur, h.Aciklama,
                                    Kisi = h.OlusturanKullanici!.Ad + " " + h.OlusturanKullanici.Soyad })
                .OrderByDescending(g => g.Key.OlusturmaZamani).Take(adet)
                .Select(g => new { g.Key, Adet = g.Count(), Ilk = g.Min(h => h.Uretim!.SeriNo) })
                .ToListAsync(ct);
            liste.AddRange(gruplar.Select(g => new IslemDto(g.Key.OlusturmaZamani, "hurda", g.Key.Kisi,
                $"{(g.Key.Tur.ToString() == "Kayip" ? "Kayıp" : "Hurda")} · {g.Adet} ürün · {g.Key.Aciklama}", g.Ilk, "seri")));
        }

        if (Al("sorun"))
        {
            liste.AddRange(await db.StokLotlari.AsNoTracking()
                .Where(l => l.Sorunlu && l.SorunZamani < sinir && (yapanId == null || l.SorunIsaretleyenId == yapanId))
                .OrderByDescending(l => l.SorunZamani).Take(adet)
                .Select(l => new IslemDto(l.SorunZamani!.Value, "sorun", l.SorunIsaretleyen!.Ad + " " + l.SorunIsaretleyen.Soyad,
                    l.Parca!.Ad + " · " + l.SorunAciklama + (l.GeriCagrildi ? " · üretime kapalı" : " · üretimde kullanılabilir"),
                    l.LotNo, "lot"))
                .ToListAsync(ct));
            liste.AddRange(await db.UrunLotSorunlari.AsNoTracking()
                .Where(s => s.OlusturmaZamani < sinir && (yapanId == null || s.OlusturanKullaniciId == yapanId))
                .OrderByDescending(s => s.OlusturmaZamani).Take(adet)
                .Select(s => new IslemDto(s.OlusturmaZamani, "sorun", s.OlusturanKullanici!.Ad + " " + s.OlusturanKullanici.Soyad,
                    s.Urun!.Ad + " · " + s.Aciklama + (s.HataliAdet > 0 ? " · " + s.HataliAdet + " hatalı" : ""), s.LotNo, "urunlot"))
                .ToListAsync(ct));
        }

        if (Al("geri"))
        {
            var gruplar = await db.UrunGeriCagirmalari.AsNoTracking()
                .Where(g => g.OlusturmaZamani < sinir && (yapanId == null || g.OlusturanKullaniciId == yapanId))
                .GroupBy(g => new { g.OlusturmaZamani, g.Aciklama, LotNo = g.StokLotu == null ? null : g.StokLotu.LotNo,
                                    Kisi = g.OlusturanKullanici!.Ad + " " + g.OlusturanKullanici.Soyad })
                .OrderByDescending(x => x.Key.OlusturmaZamani).Take(adet)
                .Select(x => new { x.Key, Adet = x.Count(), Ilk = x.Min(g => g.Uretim!.SeriNo) })
                .ToListAsync(ct);
            liste.AddRange(gruplar.Select(x => new IslemDto(x.Key.OlusturmaZamani, "geri", x.Key.Kisi,
                $"{x.Adet} ürün{(x.Key.LotNo is null ? "" : $" · {x.Key.LotNo} lotu yüzünden")} · {x.Key.Aciklama}",
                x.Key.LotNo ?? x.Ilk, x.Key.LotNo is null ? "seri" : "lot")));
        }

        return [.. liste.OrderByDescending(i => i.Zaman).Take(adet)];
    }

    private static string DuzeltmeAdi(string tur) => tur switch
    {
        "SayimFarki" => "Sayım farkı",
        "Fire" => "Fire",
        _ => "Diğer",
    };
}
