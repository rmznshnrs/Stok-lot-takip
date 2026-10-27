using LotTakip.Business;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

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
        Assert.Single(await Db.Satislar.ToListAsync());  // yeni satış yapılmaz
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
        await Geri.GeriAlAsync(id, Admin.Id);  // iade al: depoya döner

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

public class UrunLotSorunuTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private UrunLotServisi Lotlar => new(Db, new GeriCagirmaServisi(Db));

    [Fact]
    public async Task Hatali_isaretlenince_urunler_kendiliginden_geri_cagrilmaz_satista_uyari_verir()
    {
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);  // SN-IP-L001
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id);

        await Assert.ThrowsAsync<YetkiHatasi>(() => Lotlar.SorunAsync("SN-IP-L001", new(true, "x"), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Lotlar.SorunAsync("SN-IP-L001", new(true, " "), Admin.Id));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Lotlar.SorunAsync("YOK-L1", new(true, "x"), Admin.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Lotlar.SorunAsync("SN-IP-L001", new(true, "x", 4), Admin.Id));

        var s = await Lotlar.SorunAsync("sn-ip-l001", new(true, "Kasa çatlak", 2), Admin.Id);
        Assert.Empty(s.GeriCagrilanSeriNolar);
        Assert.Equal(0, await Db.UrunGeriCagirmalari.CountAsync());
        Assert.Equal(("SN-IP-L001", true, "Kasa çatlak", 2), (s.Lot.LotNo, s.Lot.Sorunlu, s.Lot.SorunAciklama, s.Lot.HataliAdet));
        Assert.Equal((3, 2, 1, 0), (s.Lot.Toplam, s.Lot.Depoda, s.Lot.Satilan, s.Lot.HurdaKayip));

        var uyari = Assert.Single(await Satis.SatisUyarilariAsync(["SN-IP-0002"]));
        Assert.Contains("SN-IP-L001", uyari.Lotlar);
        var islem = (await new IslemGecmisiServisi(Db).IslemlerAsync(Admin.Id)).Single(i => i.NumaraTuru == "urunlot");
        Assert.Equal(("sorun", "SN-IP-L001"), (islem.Tur, islem.Numara));
        Assert.Contains("2 hatalı", islem.Aciklama);
    }

    [Fact]
    public async Task Secilen_urunler_geri_cagrilir_ve_hata_kaldirilir()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);  // SN-IP-L001
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);  // SN-IP-L002

        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() =>
            Lotlar.SorunAsync("SN-IP-L001", new(true, "x", 0, ["SN-IP-0003"]), Admin.Id));
        Assert.Contains("SN-IP-0003", hata.Message);
        Assert.Equal(0, await Db.UrunLotSorunlari.CountAsync());  // yanlış seçimde işaret de kaydedilmez

        var s = await Lotlar.SorunAsync("SN-IP-L001", new(true, "Kasa çatlak", 1, ["SN-IP-0002"]), Admin.Id);
        Assert.Equal(["SN-IP-0002"], s.GeriCagrilanSeriNolar);
        Assert.Equal(1, s.Lot.AcikGeriCagirma);
        var g = Assert.Single(await new GeriCagirmaServisi(Db).ListeAsync());
        Assert.Equal(("SN-IP-0002", null, "SN-IP-L001 hatalı: Kasa çatlak"), (g.SeriNo, g.LotNo, g.Aciklama));

        // Düzenleme: açıklama ve adet değişir, kayıt tekdir; kaldırınca işaret gider, geri çağırma kalır
        s = await Lotlar.SorunAsync("SN-IP-L001", new(true, "Kasa ve kapak", 2), Admin.Id);
        Assert.Equal(("Kasa ve kapak", 2), (s.Lot.SorunAciklama, s.Lot.HataliAdet));
        Assert.Equal(1, await Db.UrunLotSorunlari.CountAsync());
        s = await Lotlar.SorunAsync("SN-IP-L001", new(false, null), Admin.Id);
        Assert.False(s.Lot.Sorunlu);
        Assert.Equal(0, await Db.UrunLotSorunlari.CountAsync());
        Assert.Equal(1, await Db.UrunGeriCagirmalari.CountAsync());

        var liste = await Lotlar.ListeAsync(Urun.Id);
        Assert.Equal(["SN-IP-L002", "SN-IP-L001"], liste.Select(l => l.LotNo));
        Assert.Empty(await Lotlar.ListeAsync(-1));
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
