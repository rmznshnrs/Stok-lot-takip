using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Concrete;

public class IzlemeServisi(AppDbContext db) : IIzlemeServisi
{
    private readonly GeriCagirmaServisi geri = new(db);

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
                HurdaTur = (UretimHurdaTuru?)t.Uretim.Hurda!.Tur,
                AcikGeri = t.Uretim.GeriCagirmalar.Any(g => g.KapanmaZamani == null),
            })
            .Select(g => new { g.Key, Adet = g.Sum(t => t.Adet) })
            .ToListAsync(ct);

        var uretimler = satirlar.OrderBy(s => s.Key.SeriNo).Select(s => new LotIzUretimDto(
            s.Key.UretimId, s.Key.SeriNo, s.Key.Tarih, s.Key.UrunKod, s.Key.UrunAd, s.Adet,
            s.Key.SatisId is null ? null : new SatisBilgisiDto(
                s.Key.SatisId.Value, s.Key.SatisTarih!.Value, s.Key.MusteriId!.Value, s.Key.MusteriAd!, s.Key.MusteriIletisim!),
            s.Key.HurdaTur?.ToString(), s.Key.AcikGeri && s.Key.HurdaTur == null ? "acik" : null))
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
            .Include(u => u.Hurda!).ThenInclude(h => h.OlusturanKullanici)
            .Include(u => u.Iadeler).ThenInclude(i => i.Satis!).ThenInclude(s => s.Musteri)
            .Include(u => u.Iadeler).ThenInclude(i => i.OlusturanKullanici)
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
            satis is null ? null : new SatisBilgisiDto(satis.Id, satis.Tarih, satis.MusteriId, satis.Musteri!.Ad, satis.Musteri.Iletisim),
            uretim.Hurda is not { } h ? null
                : new HurdaBilgisiDto(h.Tur.ToString(), h.Aciklama, h.OlusturmaZamani, $"{h.OlusturanKullanici!.Ad} {h.OlusturanKullanici.Soyad}"),
            [.. uretim.Iadeler.OrderBy(i => i.OlusturmaZamani).Select(i => new IadeBilgisiDto(i.Satis!.Musteri!.Ad, i.Satis.Tarih,
                i.OlusturmaZamani, i.Aciklama, $"{i.OlusturanKullanici!.Ad} {i.OlusturanKullanici.Soyad}"))],
            (await geri.ListeAsync(ct: ct)).Where(g => g.SeriNo == uretim.SeriNo).FirstOrDefault(), uretim.LotNo);
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
        if (await db.Uretimler.Where(u => u.LotNo == m).Select(u => u.LotNo).FirstOrDefaultAsync(ct) is { } urunLot)
            return new("urunlot", urunLot, []);
        if (await db.Parcalar.Where(p => p.Kod == m).Select(p => p.Kod).FirstOrDefaultAsync(ct) is { } kod)
            return new("parca", kod, []);
        // Tedarikçinin lot numarası: tek girişe aitse o giriş, birden fazlaysa hepsi aday
        var tedarikciLotu = await db.StokLotlari.Where(l => l.TedarikciLotNo == m).OrderBy(l => l.LotNo)
            .Select(l => l.LotNo).Take(10).ToListAsync(ct);
        if (tedarikciLotu.Count == 1)
            return new("lot", tedarikciLotu[0], []);
        if (tedarikciLotu.Count > 1)
            return new(null, null, [.. tedarikciLotu.Select(x => new AdayDto("lot", x))]);

        var adaylar = (await db.StokLotlari.Where(l => EF.Functions.Like(l.LotNo, Arama.Desen(m))).OrderBy(l => l.LotNo)
                .Select(l => l.LotNo).Take(10).ToListAsync(ct)).Select(x => new AdayDto("lot", x))
            .Concat((await db.Uretimler.Where(u => EF.Functions.Like(u.SeriNo, Arama.Desen(m))).OrderBy(u => u.SeriNo)
                .Select(u => u.SeriNo).Take(10).ToListAsync(ct)).Select(x => new AdayDto("seri", x)))
            .Concat((await db.Uretimler.Where(u => EF.Functions.Like(u.LotNo, Arama.Desen(m))).Select(u => u.LotNo)
                .Distinct().OrderBy(x => x).Take(10).ToListAsync(ct)).Select(x => new AdayDto("urunlot", x)))
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
            .Select(u => new { u.Id, u.UrunId, UrunAd = u.Urun!.Ad, u.Tarih, u.SeriNo, u.LotNo }).ToListAsync(ct);
        foreach (var g in uretimler.GroupBy(u => (u.UrunId, u.Tarih)).Take(adet))
        {
            var seriler = g.Select(u => u.SeriNo).Order().ToList();
            var lotNo = g.First().LotNo;
            hareketler.Add((lotNo.Length > 0
                ? new HareketDto("uretim", Zaman.YerelTarih(g.Key.Tarih),
                    $"{g.First().UrunAd} · {seriler.Count} adet · {Aralik(seriler)}", lotNo, lotNo, "urunlot")
                : new HareketDto("uretim", Zaman.YerelTarih(g.Key.Tarih), $"{g.First().UrunAd} · {seriler.Count} adet",
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

    public Task<LotSorunSonucuDto> LotSorunAsync(string lotNo, LotSorunIstek istek, int kullaniciId, CancellationToken ct = default)
    {
        var no = (lotNo ?? "").Trim();
        var aciklama = (istek.Aciklama ?? "").Trim();
        if (istek.Sorunlu && aciklama.Length == 0)
            throw new IsKuraliHatasi("Açıklama girin.");
        return Islem.CalistirAsync(db, async () =>
        {
            var admin = await Kullanicilar.AdminGetirAsync(db, kullaniciId, "Hatalı lot işaretleme", ct);
            var lot = await db.StokLotlari.Include(l => l.Parca).Include(l => l.Tedarikci)
                          .SingleOrDefaultAsync(l => l.LotNo == no, ct)
                      ?? throw new BulunamadiHatasi($"{no} lotu bulunamadı.");
            List<string> geriCagrilan = [];
            if (istek.Sorunlu)
            {
                if (!lot.Sorunlu)
                    (lot.SorunZamani, lot.SorunIsaretleyen) = (Zaman.Simdi(), admin);
                (lot.Sorunlu, lot.SorunAciklama, lot.GeriCagrildi) = (true, aciklama, !istek.UretimdeKullan);
                await db.SaveChangesAsync(ct);  // RowVersion: aynı anda üretim olursa yeniden denenir

                // Ürünler kendiliğinden geri çağrılmaz; yalnız seçilenler (bu lottan parça taşıyanlardan)
                var secilen = SeriListesi.Temizle(istek.GeriCagrilacaklar);
                if (secilen.Count > 0)
                {
                    var bulunan = await db.UretimTuketimleri
                        .Where(t => t.StokLotuId == lot.Id && secilen.Contains(t.Uretim!.SeriNo))
                        .Select(t => t.Uretim!.SeriNo).Distinct().ToListAsync(ct);
                    var yabanci = secilen.Where(s => !bulunan.Contains(s, StringComparer.OrdinalIgnoreCase)).ToList();
                    if (yabanci.Count > 0)
                        throw new IsKuraliHatasi($"Bu girişle ilgisi yok: {string.Join(", ", yabanci)}");
                    geriCagrilan = [.. (await geri.GeriCagirAsync(
                        new GeriCagirmaIstek(bulunan, $"{lot.LotNo} hatalı: {aciklama}", lot.LotNo), kullaniciId, ct)).Select(g => g.SeriNo)];
                }
            }
            else
            {
                (lot.Sorunlu, lot.SorunAciklama, lot.GeriCagrildi, lot.SorunZamani, lot.SorunIsaretleyenId) =
                    (false, "", false, null, null);
                await db.SaveChangesAsync(ct);
            }
            return new LotSorunSonucuDto(Donusum.Lot(lot), geriCagrilan);
        }, ct);
    }
}
