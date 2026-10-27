using LotTakip.Business;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

public class GeriCagirmaTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private GeriCagirmaServisi Geri => new(Db);

    [Fact]
    public async Task Hatali_lot_isaretlenince_yalniz_secilen_urunler_geri_cagrilir()
    {
        await Uretim.UretAsync(Urun.Id, 4, Kullanici.Id);  // 0001-0003 diyot ESKI, 0004 YENI lottan
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001", "SN-IP-0002"], new(2026, 5, 1)), Kullanici.Id);
        await Uretim.HurdayaAyirAsync(new UretimHurdaIstek(["SN-IP-0003"], "Hurda", "Kırık"), Admin.Id);  // hurda çağrılamaz

        // Seçim yoksa hiçbir ürün geri çağrılmaz; yanlış seçimde hiçbir şey değişmez
        Assert.Empty((await Izleme.LotSorunAsync("L-D-ESKI", new LotSorunIstek(true, false, "Diyot kısa devre"), Admin.Id)).GeriCagrilanSeriNolar);
        Assert.Equal(0, await Db.UrunGeriCagirmalari.CountAsync());
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Izleme.LotSorunAsync("L-D-ESKI",
            new LotSorunIstek(true, false, "Diyot kısa devre", ["SN-IP-0001", "SN-IP-0004"]), Admin.Id));
        Assert.Contains("SN-IP-0004", hata.Message);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Izleme.LotSorunAsync("L-D-ESKI",
            new LotSorunIstek(true, false, "Diyot kısa devre", ["SN-IP-0003"]), Admin.Id));
        Assert.Equal(0, await Db.UrunGeriCagirmalari.CountAsync());

        var sonuc = await Izleme.LotSorunAsync("L-D-ESKI",
            new LotSorunIstek(true, false, "Diyot kısa devre", ["sn-ip-0002", "SN-IP-0001"]), Admin.Id);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002"], sonuc.GeriCagrilanSeriNolar);
        var kayit = await Geri.ListeAsync();
        Assert.Equal(["SN-IP-0001", "SN-IP-0002"], kayit.Select(k => k.SeriNo).Order());
        Assert.All(kayit, k => Assert.Equal(("L-D-ESKI", "musteride", "L-D-ESKI hatalı: Diyot kısa devre"), (k.LotNo, k.Durum, k.Aciklama)));

        // Hata düzenlenince yeniden çağrılmaz; diğer lotun ürünü etkilenmez
        Assert.Empty((await Izleme.LotSorunAsync("L-D-ESKI", new LotSorunIstek(true, true, "Diyot kısa devre"), Admin.Id)).GeriCagrilanSeriNolar);
        Assert.Equal(2, await Db.UrunGeriCagirmalari.CountAsync());
        Assert.Equal("SN-IP-0004", (await Satis.SatilabilirAsync("SN-IP-0004")).SeriNo);

        // Müşteriden geri alındı: iade olur, depoya döner, hâlâ satışa kapalı
        var ilk = kayit.Single(k => k.SeriNo == "SN-IP-0001");
        var alinan = await Geri.GeriAlAsync(ilk.Id, Admin.Id);
        Assert.Equal(("geri_alindi", "Atlas"), (alinan.Durum, alinan.MusteriAd));
        Assert.Equal(["SN-IP-0001"], Assert.Single(await Satis.SatisGecmisiAsync()).IadeEdilenler);
        await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatilabilirAsync("SN-IP-0001"));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.GeriAlAsync(ilk.Id, Admin.Id));

        // Müşterideki satışa açılamaz; depodaki açılır
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.SatisaAcAsync(kayit.Single(k => k.SeriNo == "SN-IP-0002").Id, Admin.Id));
        Assert.Equal("satisa_acildi", (await Geri.SatisaAcAsync(ilk.Id, Admin.Id)).Durum);
        Assert.Equal("SN-IP-0001", (await Satis.SatilabilirAsync("SN-IP-0001")).SeriNo);
        Assert.Equal(["SN-IP-0002"], (await Geri.ListeAsync(acik: true, arama: "atlas")).Select(g => g.SeriNo));

        // İzlerde ve işlem geçmişinde
        Assert.Equal("musteride", (await Izleme.SeriIzleAsync("SN-IP-0002"))!.GeriCagirma!.Durum);
        Assert.Equal("acik", (await Izleme.LotIzleAsync("L-D-ESKI"))!.Uretimler.Single(u => u.SeriNo == "SN-IP-0002").GeriCagirmaDurumu);
        Assert.Equal("acik", (await Uretim.UretimGecmisiAsync("0002")).Single().GeriCagirmaDurumu);
        var islemler = await new IslemGecmisiServisi(Db).IslemlerAsync(Admin.Id);
        Assert.Contains(islemler, i => i.Tur == "sorun" && i.Numara == "L-D-ESKI");
        Assert.Contains(islemler, i => i.Tur == "geri" && i.Aciklama.Contains("2 ürün"));
    }

    [Fact]
    public async Task Tek_urun_lot_olmadan_geri_cagrilir_ve_kurallar()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        await Uretim.HurdayaAyirAsync(new UretimHurdaIstek(["SN-IP-0002"], "Kayip", "x"), Admin.Id);

        await Assert.ThrowsAsync<YetkiHatasi>(() => Geri.GeriCagirAsync(new(["SN-IP-0001"], "x"), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.GeriCagirAsync(new(["SN-IP-0001"], " "), Admin.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.GeriCagirAsync(new(["SN-IP-0001", "SN-YOK"], "x"), Admin.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.GeriCagirAsync(new(["SN-IP-0001", "SN-IP-0002"], "x"), Admin.Id)); // hurda
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Geri.GeriCagirAsync(new(["SN-IP-0001"], "x", "L-YOK"), Admin.Id));
        Assert.Equal(0, await Db.UrunGeriCagirmalari.CountAsync());

        var g = Assert.Single(await Geri.GeriCagirAsync(new(["SN-IP-0001"], "Müşteri şikâyeti"), Admin.Id));
        Assert.Null(g.LotNo);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.GeriCagirAsync(new(["SN-IP-0001"], "x"), Admin.Id)); // zaten açık
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.GeriAlAsync(g.Id, Admin.Id));                          // müşteride değil
        await Assert.ThrowsAsync<YetkiHatasi>(() => Geri.SatisaAcAsync(g.Id, Kullanici.Id));

        // Kapandıktan sonra yeniden geri çağrılabilir
        await Geri.SatisaAcAsync(g.Id, Admin.Id);
        Assert.Single(await Geri.GeriCagirAsync(new(["SN-IP-0001"], "İkinci şikâyet"), Admin.Id));
        Assert.Equal(2, await Db.UrunGeriCagirmalari.CountAsync());
    }

    [Fact]
    public async Task Kullanilmasina_izin_verilen_hatali_lottan_yeni_urun_satista_uyari_verir()
    {
        await Izleme.LotSorunAsync("L-D-ESKI", new LotSorunIstek(true, true, "Şüpheli"), Admin.Id);
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);  // işaretlemeden sonra üretilen: geri çağrılmaz
        Assert.Equal(["L-D-ESKI"], (await Satis.SatilabilirAsync("SN-IP-0001")).GeriCagrilanLotlar);
        await Assert.ThrowsAsync<GeriCagrilanLotOnayiGerekliHatasi>(() => Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id));
    }
}
