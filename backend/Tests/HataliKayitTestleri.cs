using Microsoft.EntityFrameworkCore;

namespace Tests;

public class MusteriyeGeriVermeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private GeriCagirmaServisi Geri => new(Db);

    private async Task<int> Cagir(string seriNo) =>
        (await Geri.GeriCagirAsync(new([seriNo], "Kontrol edilecek"), Admin.Id)).Single().Id;

    [Fact]
    public async Task Musterideki_urun_musteride_kalir_geri_cagirma_kapanir()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id);
        var id = await Cagir("SN-IP-0001");

        await Assert.ThrowsAsync<YetkiHatasi>(() => Geri.MusteriyeVerAsync(id, Kullanici.Id));
        var g = await Geri.MusteriyeVerAsync(id, Admin.Id);
        Assert.Equal(("musteriye_verildi", "Atlas"), (g.Durum, g.MusteriAd));
        Assert.NotNull(g.KapanmaZamani);
        Assert.Single(await Db.Satislar.ToListAsync());
        Assert.Empty(await Geri.ListeAsync(acik: true));
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.MusteriyeVerAsync(id, Admin.Id));
        Assert.Contains("kapanmış", hata.Message);
    }

    [Fact]
    public async Task Geri_alinan_urun_ayni_musteriye_yeni_satisla_gider()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"], new(2026, 5, 1)), Kullanici.Id);
        var id = await Cagir("SN-IP-0001");
        await Geri.GeriAlAsync(id, Admin.Id);

        var g = await Geri.MusteriyeVerAsync(id, Admin.Id);
        Assert.Equal(("musteriye_verildi", "Atlas"), (g.Durum, g.MusteriAd));
        var gecmis = await Satis.SatisGecmisiAsync();
        Assert.Equal(2, gecmis.Count);
        var yeni = gecmis.Single(s => s.SeriNolar.Contains("SN-IP-0001"));
        Assert.Equal(("Atlas", Zaman.Bugun()), (yeni.MusteriAd, yeni.Tarih));
        Assert.Equal("Atlas", (await Izleme.SeriIzleAsync("SN-IP-0001"))!.Satis!.MusteriAd);
    }

    [Fact]
    public async Task Hic_satilmamis_urun_musteriye_verilemez()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        var id = await Cagir("SN-IP-0001");
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.MusteriyeVerAsync(id, Admin.Id));
        Assert.Contains("müşteriden alınmamış", hata.Message);
        Assert.Equal("satisa_acildi", (await Geri.SatisaAcAsync(id, Admin.Id)).Durum);
    }
}

public class ParcaHataliAdetTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Stoktan_dusulerek_ya_da_yalniz_sayi_olarak_kaydedilir()
    {
        await Assert.ThrowsAsync<YetkiHatasi>(() => Duzeltme.HataliAsync(new(LedLot.Id, 1, true, "x"), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Duzeltme.HataliAsync(new(LedLot.Id, 0, true, "x"), Admin.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Duzeltme.HataliAsync(new(LedLot.Id, 1, true, " "), Admin.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Duzeltme.HataliAsync(new(DiyotEski.Id, 4, true, "x"), Admin.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Duzeltme.HataliAsync(new(DiyotEski.Id, 4, false, "x"), Admin.Id));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Duzeltme.HataliAsync(new(-1, 1, true, "x"), Admin.Id));

        var d = await Duzeltme.HataliAsync(new(LedLot.Id, 5, true, "Bacak kırık"), Admin.Id);
        Assert.Equal((-5, "Hatali", 95), (d.Miktar, d.Tur, d.YeniKalanAdet));
        d = await Duzeltme.HataliAsync(new(LedLot.Id, 3, false, "Renk soluk"), Admin.Id);
        Assert.Equal((0, 95), (d.Miktar, d.YeniKalanAdet));
        Assert.Equal(95, await Kalan(LedLot));

        var led = (await Stok.ParcaStoklariAsync("LED")).Single();
        Assert.Equal((8, 95), (led.HataliAdet, led.ToplamStok));
        Assert.Equal(0, (await Stok.ParcaStoklariAsync("DIY")).Single().HataliAdet);

        var islemler = (await new IslemGecmisiServisi(Db).IslemlerAsync(Admin.Id)).Where(i => i.Tur == "duzeltme").ToList();
        Assert.Contains(islemler, i => i.Aciklama.Contains("5 hatalı · stoktan düşüldü"));
        Assert.Contains(islemler, i => i.Aciklama.Contains("3 hatalı · stokta bırakıldı"));
    }

    [Fact]
    public async Task Normal_duzeltmede_hatali_turu_kullanilamaz() =>
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Duzeltme.DuzeltAsync(new(LedLot.Id, -1, "Hatali", "x"), Admin.Id));
}
