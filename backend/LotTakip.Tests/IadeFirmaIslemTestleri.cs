using LotTakip.Business;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

public class AramaTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public void Desen_ozel_karakterleri_kacirir_ve_i_harflerini_esler()
    {
        Assert.Equal("%[iİı]p%", Arama.Desen("ip"));
        Assert.Equal("%50[%] [_][[]%", Arama.Desen("50% _["));
        Assert.True(Arama.Eslesir("L-KIRMIZI-01", "kirmizi"));
        Assert.True(Arama.Eslesir("İndikatör", "indik"));
        Assert.False(Arama.Eslesir("Diyot", "led"));
    }

    [Fact]
    public async Task Turkce_i_harfleri_birbirinin_yerine_gecer()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        var tanim = new TanimServisi(Db, Stok);

        Assert.Single(await tanim.UrunleriListeleAsync("indikatör"));   // ürün "İndikatör Paneli"
        Assert.Single(await tanim.UrunleriListeleAsync("İNDİKATÖR"));
        Assert.Equal(["Diyot"], (await Stok.ParcaStoklariAsync("dıyot")).Select(p => p.Ad));
        Assert.Equal(["Diyot"], (await Stok.ParcaStoklariAsync("DİYOT")).Select(p => p.Ad));
        Assert.Equal(2, (await Uretim.UretimGecmisiAsync("sn-ip")).Count);
        Assert.Equal(2, (await Uretim.UretimGecmisiAsync("indikatör")).Count);
        Assert.Empty(await Stok.ParcaStoklariAsync("%"));                  // LIKE joker karakteri düz metin
    }
}

public class FirmaTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Musteri_eklenir_duzenlenir_ad_tekil()
    {
        var tanim = new TanimServisi(Db, Stok);
        var beta = await tanim.MusteriKaydetAsync(null, new IletisimIstek("  Beta   Enerji ", " 0212 555 00 00 "));
        Assert.Equal(("Beta Enerji", "0212 555 00 00"), (beta.Ad, beta.Iletisim));

        await Assert.ThrowsAsync<IsKuraliHatasi>(() => tanim.MusteriKaydetAsync(null, new IletisimIstek("beta enerji", "")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => tanim.MusteriKaydetAsync(beta.Id, new IletisimIstek("Atlas", "")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => tanim.MusteriKaydetAsync(beta.Id, new IletisimIstek(" ", "")));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => tanim.MusteriKaydetAsync(9999, new IletisimIstek("X", "")));

        var duzelen = await tanim.MusteriKaydetAsync(Musteri.Id, new IletisimIstek("Atlas Otomasyon", "satin@atlas.example"));
        Assert.Equal(Musteri.Id, duzelen.Id);
        var liste = await tanim.MusterilerAsync();
        Assert.Equal(["Atlas Otomasyon", "Beta Enerji"], liste.Select(m => m.Ad));
        Assert.Equal("satin@atlas.example", liste[0].Iletisim);
    }

    [Fact]
    public async Task Tedarikci_eklenir_duzenlenir_ad_tekil()
    {
        var tanim = new TanimServisi(Db, Stok);
        var yeni = await tanim.TedarikciKaydetAsync(null, new IletisimIstek("Elektro A.Ş.", null));
        Assert.Equal("", yeni.Iletisim);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => tanim.TedarikciKaydetAsync(yeni.Id, new IletisimIstek(Ted.Ad, "")));
        await tanim.TedarikciKaydetAsync(Ted.Id, new IletisimIstek(Ted.Ad, "0216 444 00 00"));
        Assert.Equal("0216 444 00 00", (await tanim.TedarikcilerAsync()).Single(t => t.Id == Ted.Id).Iletisim);
    }
}

public class SatisIadeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Iade_alinan_urun_depoya_doner_ve_yeniden_satilir()
    {
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);  // 0001-0003 diyot ESKI lottan
        var satis = await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001", "SN-IP-0002"], new(2026, 5, 1)), Kullanici.Id);

        var s = await Satis.IadeAlAsync(satis.SatisId, new SatisIadeIstek(["sn-ip-0002"], " Arızalı geldi "), Admin.Id);
        Assert.Equal(["SN-IP-0002"], s.SeriNolar);

        var g = Assert.Single(await Satis.SatisGecmisiAsync());
        Assert.Equal(["SN-IP-0001"], g.SeriNolar);
        Assert.Equal(["SN-IP-0002"], g.IadeEdilenler);
        Assert.Single(await Satis.SatisGecmisiAsync("0002"));               // iade edilen de aranır

        var iz = (await Izleme.SeriIzleAsync("SN-IP-0002"))!;
        Assert.Null(iz.Satis);
        var iade = Assert.Single(iz.Iadeler!);
        Assert.Equal(("Atlas", new DateOnly(2026, 5, 1), "Arızalı geldi", "Ad Min"), (iade.MusteriAd, iade.SatisTarihi, iade.Aciklama, iade.KullaniciAd));
        Assert.Contains("SN-IP-0002", (await Izleme.LotIzleAsync("L-D-ESKI"))!.StoktakiUretimler.Select(u => u.SeriNo));

        // Yeniden satılabilir; iade geçmişi kalır
        Assert.Equal("SN-IP-0002", (await Satis.SatilabilirAsync("SN-IP-0002")).SeriNo);
        await Satis.SatAsync(new SatisIstek("Beta", ["SN-IP-0002"], new(2026, 6, 1)), Kullanici.Id);
        iz = (await Izleme.SeriIzleAsync("SN-IP-0002"))!;
        Assert.Equal("Beta", iz.Satis!.MusteriAd);
        Assert.Equal("Atlas", Assert.Single(iz.Iadeler!).MusteriAd);
        var lot = (await Izleme.LotIzleAsync("L-D-ESKI"))!;
        Assert.Equal(["Atlas", "Beta"], lot.Musteriler.Select(m => m.Ad));
    }

    [Fact]
    public async Task Kurallar_ve_hepsi_ya_da_hicbiri()
    {
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);
        var satis = await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001", "SN-IP-0002"]), Kullanici.Id);

        await Assert.ThrowsAsync<YetkiHatasi>(() => Satis.IadeAlAsync(satis.SatisId, new(["SN-IP-0001"], "x"), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Satis.IadeAlAsync(satis.SatisId, new(["SN-IP-0001"], " "), Admin.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Satis.IadeAlAsync(satis.SatisId, new([], "x"), Admin.Id));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Satis.IadeAlAsync(9999, new(["SN-IP-0001"], "x"), Admin.Id));
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() =>
            Satis.IadeAlAsync(satis.SatisId, new(["SN-IP-0001", "SN-IP-0003"], "x"), Admin.Id));
        Assert.Contains("SN-IP-0003", hata.Message);
        Assert.Equal(0, await Db.SatisIadeleri.CountAsync());
        Assert.Equal(2, await Db.SatisKalemleri.CountAsync());

        await Satis.IadeAlAsync(satis.SatisId, new(["SN-IP-0001", "SN-IP-0002"], "Sipariş iptal"), Admin.Id);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Satis.IadeAlAsync(satis.SatisId, new(["SN-IP-0001"], "x"), Admin.Id));
        var g = Assert.Single(await Satis.SatisGecmisiAsync());           // kalemsiz satış da görünür
        Assert.Empty(g.SeriNolar);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002"], g.IadeEdilenler);
        Assert.DoesNotContain(await Izleme.SonHareketlerAsync(), h => h.Tur == "satis");
    }
}

public class IslemGecmisiTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Kim_ne_yapti_listelenir_ve_suzulur()
    {
        var islemler = new IslemGecmisiServisi(Db);
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);
        var satis = await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id);
        await Satis.IadeAlAsync(satis.SatisId, new(["SN-IP-0001"], "Arızalı"), Admin.Id);
        await Duzeltme.DuzeltAsync(new StokDuzeltmeIstek(LedLot.Id, -2, "Fire", "Kırıldı"), Admin.Id);
        await Uretim.HurdayaAyirAsync(new UretimHurdaIstek(["SN-IP-0002", "SN-IP-0003"], "Hurda", "Düştü"), Admin.Id);

        var hepsi = await islemler.IslemlerAsync(Admin.Id);
        Assert.Equal(["duzeltme", "giris", "hurda", "iade", "satis", "uretim"], hepsi.Select(i => i.Tur).Distinct().Order());
        Assert.Equal(hepsi.OrderByDescending(i => i.Zaman).Select(i => i.Zaman), hepsi.Select(i => i.Zaman));
        var uretim = Assert.Single(hepsi, i => i.Tur == "uretim");      // 3 adet tek satır
        Assert.Contains("3 adet", uretim.Aciklama);
        Assert.Equal(("Depo Görevlisi", "SN-IP-0001", "seri"), (uretim.KullaniciAd, uretim.Numara, uretim.NumaraTuru));
        var hurda = Assert.Single(hepsi, i => i.Tur == "hurda");
        Assert.Contains("2 ürün", hurda.Aciklama);
        var duzeltme = Assert.Single(hepsi, i => i.Tur == "duzeltme");
        Assert.Equal(("Ad Min", "L-LED", "lot"), (duzeltme.KullaniciAd, duzeltme.Numara, duzeltme.NumaraTuru));
        Assert.Contains("-2", duzeltme.Aciklama);

        Assert.Equal(["iade"], (await islemler.IslemlerAsync(Admin.Id, tur: "iade")).Select(i => i.Tur));
        Assert.All(await islemler.IslemlerAsync(Admin.Id, yapanId: Admin.Id), i => Assert.Equal("Ad Min", i.KullaniciAd));
        Assert.Equal(2, (await islemler.IslemlerAsync(Admin.Id, adet: 2)).Count);
        var son = hepsi[1].Zaman;
        Assert.All(await islemler.IslemlerAsync(Admin.Id, once: son), i => Assert.True(i.Zaman < son));

        await Assert.ThrowsAsync<YetkiHatasi>(() => islemler.IslemlerAsync(Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => islemler.IslemlerAsync(Admin.Id, tur: "silme"));
    }
}
