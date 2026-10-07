using LotTakip.Business;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

public class StokTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Toplam_kullanilabilir_lot_sayisi_ve_min()
    {
        await Db.Parcalar.Where(p => p.Id == Diyot.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.MinStok, 20));
        Db.Parcalar.Add(new Parca { Kod = "YENI", Ad = "Yeni parça" });
        await Db.SaveChangesAsync();
        var p = (await Stok.ParcaStoklariAsync()).ToDictionary(x => x.Kod);
        Assert.Equal(63, p["DIY"].ToplamStok);         // 10 + 3 + 50 (geri çağrılan)
        Assert.Equal(13, p["DIY"].KullanilabilirStok);
        Assert.Equal(3, p["DIY"].LotSayisi);
        Assert.True(p["DIY"].MinAltinda);
        Assert.False(p["LED"].MinAltinda);
        Assert.Equal(["İndikatör Paneli"], p["LED"].KullanildigiUrunler);
        Assert.Equal((0, 0), (p["YENI"].ToplamStok, p["YENI"].LotSayisi));
        Assert.Empty(p["YENI"].KullanildigiUrunler);
        Assert.Equal(["DIY"], (await Stok.ParcaStoklariAsync("diyot")).Select(x => x.Kod));
    }

    [Fact]
    public async Task Bitmis_lot_sayilmaz()
    {
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);  // L-D-ESKI biter
        Assert.Equal(2, (await Stok.ParcaStoklariAsync()).Single(p => p.Kod == "DIY").LotSayisi);
    }

    [Fact]
    public async Task Mal_girisi_lot_olusur_kalan_giris_adedine_esit()
    {
        var once = Zaman.Simdi();
        var s = await Stok.MalGirisiAsync(new MalGirisiIstek(Diyot.Id, "tedarikçi a", " L-YENI ", 40, new(2026, 9, 1), "SP-9"), Kullanici.Id);
        Assert.False(s.YeniTedarikci);  // büyük/küçük harf duyarsız eşleşti
        Assert.Equal(("L-YENI", 40, 40, "Tedarikçi A", "SP-9"), (s.Lot.LotNo, s.Lot.GirisAdet, s.Lot.KalanAdet, s.Lot.TedarikciAd, s.Lot.SiparisNo));
        var lot = await Db.StokLotlari.AsNoTracking().SingleAsync(l => l.LotNo == "L-YENI");
        Assert.Equal(Kullanici.Id, lot.OlusturanKullaniciId);
        Assert.True(lot.GirisZamani >= once.AddSeconds(-1));
    }

    [Fact]
    public async Task Mal_girisi_yeni_tedarikci_olusur()
    {
        var s = await Stok.MalGirisiAsync(new MalGirisiIstek(Diyot.Id, "  Yeni   Firma ", "L-X", 1, new(2026, 9, 1)), Kullanici.Id);
        Assert.True(s.YeniTedarikci);
        Assert.Equal("Yeni Firma", s.Lot.TedarikciAd);
        s = await Stok.MalGirisiAsync(new MalGirisiIstek(Diyot.Id, "yeni firma", "L-Y", 1, new(2026, 9, 1)), Kullanici.Id);
        Assert.False(s.YeniTedarikci);
    }

    [Theory]
    [InlineData("L-LED", 1)]
    [InlineData("l-led", 1)]
    [InlineData("L-Z", 0)]
    [InlineData("", 1)]
    public async Task Mal_girisi_hatalari(string lotNo, int adet)
    {
        await Assert.ThrowsAsync<IsKuraliHatasi>(
            () => Stok.MalGirisiAsync(new MalGirisiIstek(Led.Id, "Tedarikçi A", lotNo, adet, new(2026, 9, 1)), Kullanici.Id));
        Assert.Equal(4, await Db.StokLotlari.CountAsync());
    }

    [Fact]
    public async Task Mal_girisi_bilinmeyen_parca() =>
        await Assert.ThrowsAsync<BulunamadiHatasi>(
            () => Stok.MalGirisiAsync(new MalGirisiIstek(-1, "T", "L-Q", 1, new(2026, 9, 1)), Kullanici.Id));

    private async Task<Dictionary<string, StokGrubuDto>> Gruplar(string? arama = null, string? parca = null, bool bitenler = false) =>
        (await Stok.StokDurumuAsync(arama, parca, bitenler)).ToDictionary(g => g.Parca.Kod);

    [Fact]
    public async Task Stok_durumu_fifo_sirasi_ve_siradaki_lot()
    {
        var g = (await Gruplar())["DIY"];
        Assert.Equal(["L-D-GERI", "L-D-ESKI", "L-D-YENI"], g.Lotlar.Select(l => l.LotNo));
        Assert.Equal("L-D-ESKI", g.Siradaki!.LotNo);  // geri çağrılan sıradaki olmaz
        Assert.Equal([false, true, false], g.Lotlar.Select(l => l.Sirada));
        Assert.Equal((63, 13), (g.Parca.ToplamStok, g.Parca.KullanilabilirStok));
    }

    [Fact]
    public async Task Stok_durumu_biten_lotlar_gizlenir()
    {
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);  // L-D-ESKI biter
        var g = (await Gruplar())["DIY"];
        Assert.Equal(["L-D-GERI", "L-D-YENI"], g.Lotlar.Select(l => l.LotNo));
        Assert.Equal("L-D-YENI", g.Siradaki!.LotNo);
        Assert.Equal(3, (await Gruplar(bitenler: true))["DIY"].Lotlar.Count);
    }

    [Fact]
    public async Task Stok_durumu_arama()
    {
        Assert.Equal(["DIY"], (await Gruplar("diyot")).Keys);
        Assert.Equal(3, (await Gruplar("diyot"))["DIY"].Lotlar.Count);
        var g = await Gruplar("eski");  // yalnız lot eşleşirse yalnız o lot
        Assert.Equal(["DIY"], g.Keys);
        Assert.Equal(["L-D-ESKI"], g["DIY"].Lotlar.Select(l => l.LotNo));
        Assert.Empty(await Gruplar("yok-boyle-bir-sey"));
    }

    [Fact]
    public async Task Stok_durumu_parca_filtresi() => Assert.Equal(["LED"], (await Gruplar(parca: "led")).Keys);

    [Fact]
    public async Task Parca_bul_kod_ad_kismi_ve_hatalar()
    {
        Assert.Equal("LED", (await Stok.ParcaBulAsync(" led ")).Kod);
        Assert.Equal("DIY", (await Stok.ParcaBulAsync("diyot")).Kod);
        Assert.Equal("DIY", (await Stok.ParcaBulAsync("DIY – Diyot")).Kod);
        Assert.Equal("DIY", (await Stok.ParcaBulAsync("iyo")).Kod);
        Db.Parcalar.Add(new Parca { Kod = "DIY-2", Ad = "Diyot 2" });
        await Db.SaveChangesAsync();
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Stok.ParcaBulAsync("iyo"));
        Assert.Contains("birden fazla parçayla eşleşti: DIY, DIY-2", hata.Message);
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Stok.ParcaBulAsync("yok"));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Stok.ParcaBulAsync("  "));
    }
}

public class StokDuzeltmeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Admin_duzeltir_kayit_ve_kalan_guncellenir()
    {
        var s = await Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(DiyotYeni.Id, -2, "fire", " kırık geldi "), Admin.Id);
        Assert.Equal((8, "Fire", "kırık geldi"), (s.YeniKalanAdet, s.Tur, s.Aciklama));
        Assert.Equal(8, await Kalan(DiyotYeni));
        var kayit = await Db.StokDuzeltmeleri.AsNoTracking().SingleAsync();
        Assert.Equal((-2, StokDuzeltmeTuru.Fire, Admin.Id), (kayit.Miktar, kayit.Tur, kayit.OlusturanKullaniciId));

        // Sayım fazlası giriş adedini aşabilir
        s = await Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(DiyotYeni.Id, 5, "SayimFarki", "sayımda fazla çıktı"), Admin.Id);
        Assert.Equal(13, s.YeniKalanAdet);
    }

    [Fact]
    public async Task Kalan_negatife_dusemez()
    {
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(
            () => Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(DiyotEski.Id, -4, "Fire", "x"), Admin.Id));
        Assert.Contains("3 adet var", hata.Message);
        Assert.Equal(3, await Kalan(DiyotEski));
        Assert.False(await Db.StokDuzeltmeleri.AnyAsync());
    }

    [Theory]
    [InlineData(0, "Fire", "x")]
    [InlineData(-1, "Fire", "  ")]
    [InlineData(-1, "Kayip", "x")]
    [InlineData(-1, "7", "x")]
    public async Task Gecersiz_istek(int miktar, string tur, string aciklama) =>
        await Assert.ThrowsAsync<IsKuraliHatasi>(
            () => Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(DiyotEski.Id, miktar, tur, aciklama), Admin.Id));

    [Fact]
    public async Task Yalniz_admin_yapar()
    {
        await Assert.ThrowsAsync<YetkiHatasi>(
            () => Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(DiyotEski.Id, -1, "Fire", "x"), Kullanici.Id));
        Assert.Equal(3, await Kalan(DiyotEski));
    }

    [Fact]
    public async Task Duzeltilen_stok_uretimde_kullanilir()
    {
        await Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(DiyotYeni.Id, 1, "SayimFarki", "fazla"), Admin.Id);  // 13 → 14
        await Uretim.UretAsync(Urun.Id, 14, Kullanici.Id);  // önceden 1 eksikti
        Assert.Equal((0, 0), (await Kalan(DiyotEski), await Kalan(DiyotYeni)));
    }

    [Fact]
    public async Task Bilinmeyen_lot() =>
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(-1, -1, "Fire", "x"), Admin.Id));
}

public class IzlemeYardimciTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Numara_birebir_eslesme()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        Assert.Equal(new NumaraAramaDto("lot", "L-D-ESKI", []).ToString(), (await Izleme.NumaraAraAsync(" l-d-eski ")).ToString());
        var s = await Izleme.NumaraAraAsync("sn-ip-0002");
        Assert.Equal(("seri", "SN-IP-0002"), (s.Tur, s.Deger));
        s = await Izleme.NumaraAraAsync("led");
        Assert.Equal(("parca", "LED"), (s.Tur, s.Deger));
    }

    [Fact]
    public async Task Numara_kismi_eslesme()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        Assert.Equal("L-D-ESKI", (await Izleme.NumaraAraAsync("ESKI")).Deger);  // tek aday → doğrudan
        var s = await Izleme.NumaraAraAsync("SN-IP");
        Assert.Null(s.Tur);
        Assert.Equal([("seri", "SN-IP-0001"), ("seri", "SN-IP-0002")], s.Adaylar.Select(a => (a.Tur, a.Deger)));
        Assert.Equal([("lot", "L-D-ESKI"), ("lot", "L-D-GERI"), ("lot", "L-D-YENI")],
                     (await Izleme.NumaraAraAsync("L-D")).Adaylar.Select(a => (a.Tur, a.Deger)));
    }

    [Fact]
    public async Task Numara_bulunamadi_ve_bos()
    {
        var s = await Izleme.NumaraAraAsync("XYZ");
        Assert.Equal((null, null, 0), (s.Tur, s.Deger, s.Adaylar.Count));
        Assert.Null((await Izleme.NumaraAraAsync("  ")).Tur);
    }

    [Fact]
    public async Task Son_hareketler_giris_uretim_satis_yeniden_eskiye()
    {
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001", "SN-IP-0002"]), Kullanici.Id);
        var h = await Izleme.SonHareketlerAsync(20);
        Assert.Equal(["satis", "uretim"], h.Take(2).Select(x => x.Tur));
        Assert.Equal("SN-IP-0001 … SN-IP-0002", h[0].Numara);
        Assert.Contains("Atlas", h[0].Aciklama);
        Assert.Equal(("İndikatör Paneli · 3 adet", "SN-IP-0001 … SN-IP-0003", "SN-IP-0001"), (h[1].Aciklama, h[1].Numara, h[1].NumaraIlk));
        Assert.Equal(4, h.Count(x => x.Tur == "giris"));
    }

    [Fact]
    public async Task Son_hareketler_giris_zamanina_gore()
    {
        // En eski siparişli lot en son girilmiş olsun
        await Db.StokLotlari.Where(l => l.Id == DiyotGeri.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.GirisZamani, Zaman.Simdi().AddHours(1)));
        await Db.StokLotlari.Where(l => l.Id == LedLot.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.GirisZamani, Zaman.Simdi().AddDays(-30)));
        var girisler = (await Izleme.SonHareketlerAsync(20)).Where(x => x.Tur == "giris").Select(x => x.Numara).ToList();
        Assert.Equal("L-D-GERI", girisler[0]);
        Assert.Equal("L-LED", girisler[^1]);
    }

    [Fact]
    public async Task Son_hareketler_limit() => Assert.Equal(2, (await Izleme.SonHareketlerAsync(2)).Count);

    [Fact]
    public async Task Lot_geri_cagir_ve_kaldir()
    {
        var lot = await Izleme.LotGeriCagirAsync("l-d-eski", geriCagir: true, Admin.Id);
        Assert.True(lot.GeriCagrildi);
        Assert.True(await Db.StokLotlari.AsNoTracking().Where(l => l.Id == DiyotEski.Id).Select(l => l.GeriCagrildi).SingleAsync());
        // Geri çağrılan lot artık sıradaki değil ve üretimde atlanır
        Assert.Equal("L-D-YENI", (await Stok.StokDurumuAsync(parcaKod: "DIY")).Single().Siradaki!.LotNo);
        await Izleme.LotGeriCagirAsync("L-D-ESKI", geriCagir: false, Admin.Id);
        Assert.False(await Db.StokLotlari.AsNoTracking().Where(l => l.Id == DiyotEski.Id).Select(l => l.GeriCagrildi).SingleAsync());
    }

    [Fact]
    public async Task Lot_geri_cagirma_yalniz_admin_ve_bilinmeyen_lot()
    {
        await Assert.ThrowsAsync<YetkiHatasi>(() => Izleme.LotGeriCagirAsync("L-D-ESKI", true, Kullanici.Id));
        Assert.False(await Db.StokLotlari.AsNoTracking().Where(l => l.Id == DiyotEski.Id).Select(l => l.GeriCagrildi).SingleAsync());
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Izleme.LotGeriCagirAsync("YOK", true, Admin.Id));
    }
}
