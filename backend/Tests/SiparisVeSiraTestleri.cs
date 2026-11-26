using Microsoft.EntityFrameworkCore;

namespace Tests;

public class UretimSirasiTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private IUretimSirasiServisi Sira = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sira = new UretimSirasiServisi(Db, new StokLotuRepository(Db), new UretimRepository(Db));
    }

    [Fact]
    public async Task Siraya_girince_parcalar_ayrilir_uretilince_dusulur()
    {
        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 10), Kullanici.Id);
        Assert.Equal(("Bekliyor", 1), (e.Durum, e.Sira));
        Assert.Equal(10, e.Parcalar.Single(p => p.Parca.Kod == "DIY").Ayrilan);
        Assert.Equal(3, (await Uretim.OnizleAsync(Urun.Id, 5)).Parcalar.Single(p => p.Parca.Kod == "DIY").Kullanilabilir);

        await Assert.ThrowsAsync<YetersizStokHatasi>(() => Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 4), Kullanici.Id));
        await Assert.ThrowsAsync<YetersizStokHatasi>(() => Uretim.UretAsync(Urun.Id, 4, Kullanici.Id));

        var s = await Sira.UretAsync(e.Id, new EmirUretIstek(), Kullanici.Id);
        Assert.Equal(["SN-IP-0001", "SN-IP-0010"], new[] { s.SeriNolar[0], s.SeriNolar[^1] });
        var bitti = await Sira.GetirAsync(e.Id);
        Assert.Equal(("Tamamlandi", 10, 0), (bitti.Durum, bitti.Uretilen, bitti.Parcalar.Sum(p => p.Ayrilan)));
        Assert.Equal(10, bitti.SeriNolar.Count);
        Assert.Empty(await Sira.ListeAsync());
        Assert.Equal(3, (await Uretim.OnizleAsync(Urun.Id, 3)).Parcalar.Single(p => p.Parca.Kod == "DIY").Kullanilabilir);
    }

    [Fact]
    public async Task Kismi_uretim_ve_iptal()
    {
        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 5), Kullanici.Id);
        await Sira.UretAsync(e.Id, new EmirUretIstek(2), Kullanici.Id);
        var ara = await Sira.GetirAsync(e.Id);
        Assert.Equal(("Bekliyor", 2, 3), (ara.Durum, ara.Uretilen, ara.Parcalar.Single(p => p.Parca.Kod == "DIY").Ayrilan));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.UretAsync(e.Id, new EmirUretIstek(4), Kullanici.Id));

        var kapandi = await Sira.IptalAsync(e.Id, Kullanici.Id);
        Assert.Equal(("Tamamlandi", 2), (kapandi.Durum, kapandi.Adet));
        await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 11), Kullanici.Id);
        await Assert.ThrowsAsync<YetersizStokHatasi>(() => Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1), Kullanici.Id));
    }

    [Fact]
    public async Task Uretime_secilen_adet_kadar_baslanir()
    {
        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 3), Kullanici.Id);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.BaslaAsync(e.Id, adet: 4));
        var baslayan = await Sira.BaslaAsync(e.Id, adet: 1);
        Assert.NotEqual(e.Id, baslayan.Id);
        Assert.Equal(1, baslayan.Adet);
        Assert.NotNull(baslayan.BaslamaZamani);
        var kalan = await Sira.GetirAsync(e.Id);
        Assert.Equal(2, kalan.Adet);
        Assert.Null(kalan.BaslamaZamani);
        Assert.Equal(3 * kalan.Parcalar[0].BirimAdet, kalan.Parcalar[0].Ayrilan + baslayan.Parcalar[0].Ayrilan);
        await Sira.UretAsync(baslayan.Id, new EmirUretIstek(), Kullanici.Id);
        Assert.Equal("Tamamlandi", (await Sira.GetirAsync(baslayan.Id)).Durum);
        Assert.Equal(2, (await Sira.BaslaAsync(e.Id)).Adet);
    }

    [Fact]
    public async Task Uretime_baslaninca_uretimde_olur()
    {
        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 2), Kullanici.Id);
        Assert.Null(e.BaslamaZamani);
        e = await Sira.BaslaAsync(e.Id);
        Assert.NotNull(e.BaslamaZamani);
        Assert.Equal(0, e.Uretilen);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.BaslaAsync(e.Id));
        await Sira.UretAsync(e.Id, new EmirUretIstek(), Kullanici.Id);
        Assert.Equal("Tamamlandi", (await Sira.GetirAsync(e.Id)).Durum);
    }

    [Fact]
    public async Task Parca_gelince_eksik_talep_uretim_sirasina_girer()
    {
        var siparis = await new SiparisServisi(Db).OlusturAsync(new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 40)]), Kullanici.Id);
        await Assert.ThrowsAsync<YetersizStokHatasi>(() => Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 40, siparis.Kalemler[0].Id), Kullanici.Id));
        var once = (await Sira.ListeAsync()).Count;
        var giris = await Stok.MalGirisiAsync(new MalGirisiIstek(Diyot.Id, "Tedarikçi A", "L-OTO-1", 500, new(2026, 11, 1)), Kullanici.Id);
        var giren = Assert.Single(giris.SirayaGirenler!);
        Assert.Contains("Atlas", giren);
        var emir = Assert.Single(await Sira.ListeAsync(), e => e.SiparisId == siparis.Id);
        Assert.True(emir.Adet >= 1);
        Assert.Equal(once + 1, (await Sira.ListeAsync()).Count);
    }

    [Fact]
    public async Task Seri_no_elle_girilir_tekrar_kabul_edilmez()
    {
        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 2), Kullanici.Id);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.UretAsync(e.Id, new EmirUretIstek(2, ["EL-1"]), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.UretAsync(e.Id, new EmirUretIstek(2, ["EL-1", "el-1"]), Kullanici.Id));
        var sonuc = await Sira.UretAsync(e.Id, new EmirUretIstek(1, [" el-1 "]), Kullanici.Id);
        Assert.Equal(["EL-1"], sonuc.SeriNolar);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.UretAsync(e.Id, new EmirUretIstek(1, ["EL-1"]), Kullanici.Id));
        Assert.Single((await Sira.UretAsync(e.Id, new EmirUretIstek(1), Kullanici.Id)).SeriNolar);
    }

    [Fact]
    public async Task Sira_tasinir()
    {
        var a = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1), Kullanici.Id);
        var b = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1), Kullanici.Id);
        var liste = await Sira.TasiAsync(b.Id, -1);
        Assert.Equal([b.Id, a.Id], liste.Select(x => x.Id));
        Assert.Equal([b.Id, a.Id], (await Sira.TasiAsync(b.Id, -1)).Select(x => x.Id));
        var plan = await Sira.PlanlaAsync(a.Id, new(2026, 12, 1));
        Assert.Equal(new DateOnly(2026, 12, 1), plan.PlanlananTarih);
    }
    [Fact]
    public async Task Sira_istenen_konuma_tek_seferde_tasinir()
    {
        var a = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1), Kullanici.Id);
        var b = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1), Kullanici.Id);
        var c = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1), Kullanici.Id);
        Assert.Equal([c.Id, a.Id, b.Id], (await Sira.KonumaTasiAsync(c.Id, a.Id)).Select(x => x.Id));
        Assert.Equal([a.Id, b.Id, c.Id], (await Sira.KonumaTasiAsync(c.Id, b.Id)).Select(x => x.Id));
        Assert.Equal([a.Id, b.Id, c.Id], (await Sira.KonumaTasiAsync(b.Id, b.Id)).Select(x => x.Id));
        Assert.Equal([b.Id, c.Id, a.Id], (await Sira.KonumaTasiAsync(a.Id, c.Id)).Select(x => x.Id));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Sira.KonumaTasiAsync(a.Id, 9999));
    }
}

public class SiparisTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private IUretimSirasiServisi Sira = null!;
    private ISiparisServisi Siparis = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sira = new UretimSirasiServisi(Db, new StokLotuRepository(Db), new UretimRepository(Db));
        Siparis = new SiparisServisi(Db);
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id);
    }

    [Fact]
    public async Task Talep_urunleri_uretilince_kargoya_verilecek_olur_siparis_no_talepte()
    {
        var s = await Siparis.OlusturAsync(new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 3, ["SN-IP-0001"])],
            SiparisNo: " MS-1001 "), Kullanici.Id);
        Assert.Equal("MS-1001", s.SiparisNo);
        var k = s.Kalemler[0];
        var e1 = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1, k.Id), Kullanici.Id);
        await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1, k.Id), Kullanici.Id);
        var depodaOnce = (await new OzetServisi(Db, Stok).AnaSayfaAsync()).DepodaUrun;
        var uretilen = await Sira.UretAsync(e1.Id, new EmirUretIstek(), Kullanici.Id);
        Assert.NotNull(uretilen.SevkiyatId);
        var sevkiyat = new SevkiyatServisi(Db);
        var otomatik = await sevkiyat.GetirAsync(uretilen.SevkiyatId!.Value);
        Assert.Equal(("Hazirlaniyor", "MS-1001"), (otomatik.Durum, otomatik.SiparisNo));
        Assert.Equal(["SN-IP-0001", "SN-IP-0004"], otomatik.Kalemler.Select(x => x.SeriNo));
        Assert.Equal(depodaOnce + 1, (await new OzetServisi(Db, Stok).AnaSayfaAsync()).DepodaUrun);
        Assert.Equal("kargoya_verilecek", (await Siparis.GetirAsync(s.Id)).Durum);
        var sv = await sevkiyat.KargoAsync(otomatik.Id, new KargoIstek("Aras Kargo", "777", ["SN-IP-0004"]));
        Assert.Equal("kismen_gonderildi", (await Siparis.GetirAsync(s.Id)).Durum);
        Assert.Equal(depodaOnce, (await new OzetServisi(Db, Stok).AnaSayfaAsync()).DepodaUrun);

        var u = await Siparis.UrunlerAsync(s.Id);
        Assert.Equal([("Kargoda", "SN-IP-0004"), ("Hazirlaniyor", "SN-IP-0001"), ("sirada", null)],
            u.Select(x => (x.Durum, x.SeriNo)));
        Assert.Equal((sv.No, "Aras Kargo", "777"), (u[0].SevkiyatNo, u[0].KargoFirmasi, u[0].TakipNo));
        Assert.NotNull(u[0].UretimZamani);
        Assert.Equal(1, u[2].Adet);
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Siparis.UrunlerAsync(999));
    }

    [Fact]
    public async Task Hazir_seri_ayrilir_eksik_siradan_uretilip_kaleme_gelir()
    {
        var s = await Siparis.OlusturAsync(new SiparisIstek(Musteri.Id,
            [new SiparisKalemiIstek(Urun.Id, 5, ["sn-ip-0001", "SN-IP-0002"])], IstenenTarih: new(2026, 12, 15)), Kullanici.Id);
        Assert.Equal(("SP-0001", "kargoya_verilecek"), (s.No, s.Durum));
        var k = Assert.Single(s.Kalemler);
        Assert.Equal((2, 0, 3), (k.Ayrilanlar.Count, k.Uretimde, k.Eksik));
        Assert.Equal(["SN-IP-0003"], (await Siparis.HazirSerilerAsync(Urun.Id)).Select(h => h.SeriNo));

        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, null, k.Id), Kullanici.Id);
        Assert.Equal((3, s.Tarih, "Atlas"), (e.Adet, e.TalepTarihi, e.MusteriAd));
        s = await Siparis.GetirAsync(s.Id);
        Assert.Equal(("kargoya_verilecek", 0), (s.Durum, s.Kalemler[0].Eksik));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1, k.Id), Kullanici.Id));

        await Sira.UretAsync(e.Id, new EmirUretIstek(1), Kullanici.Id);
        await Sira.UretAsync(e.Id, new EmirUretIstek(2), Kullanici.Id);
        s = await Siparis.GetirAsync(s.Id);
        Assert.Equal("kargoya_verilecek", s.Durum);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0004", "SN-IP-0005", "SN-IP-0006"], s.Kalemler[0].Ayrilanlar);
        Assert.Equal(["SN-IP-0003"], (await Siparis.HazirSerilerAsync(Urun.Id)).Select(h => h.SeriNo));

        await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatAsync(new SatisIstek("Beta", ["SN-IP-0004"]), Kullanici.Id));
        var bekleyen = Assert.Single(await new SevkiyatServisi(Db).ListeAsync(durum: "Hazirlaniyor"));
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0004", "SN-IP-0005", "SN-IP-0006"], bekleyen.Kalemler.Select(x => x.SeriNo));
        var giden = await new SevkiyatServisi(Db).KargoAsync(bekleyen.Id, new KargoIstek("Aras Kargo", "1", ["sn-ip-0004"], "SV-EL-1"));
        Assert.Equal(("SV-EL-1", "Kargoda"), (giden.No, giden.Durum));
        Assert.Equal(["SN-IP-0004"], giden.Kalemler.Select(x => x.SeriNo));
        var kalan = Assert.Single(await new SevkiyatServisi(Db).ListeAsync(durum: "Hazirlaniyor"));
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0005", "SN-IP-0006"], kalan.Kalemler.Select(x => x.SeriNo));
        Assert.Null(kalan.No);
        s = await Siparis.GetirAsync(s.Id);
        Assert.Equal("kismen_gonderildi", s.Durum);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => new SevkiyatServisi(Db).KargoAsync(kalan.Id, new KargoIstek("Aras Kargo", "2", ["SN-IP-0004"])));
        await new SevkiyatServisi(Db).KargoAsync(kalan.Id, new KargoIstek("Aras Kargo", "2"));
        s = await Siparis.GetirAsync(s.Id);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0004", "SN-IP-0005", "SN-IP-0006"], s.Kalemler[0].Gonderilenler);
        Assert.Equal("tamamlandi", s.Durum);
        Assert.Empty(await Siparis.ListeAsync(Musteri.Id, acik: true));
    }

    [Fact]
    public async Task Ayrilmis_seri_baska_kaleme_ayrilamaz_birakilinca_serbest()
    {
        var a = await Siparis.OlusturAsync(new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 1, ["SN-IP-0001"])]), Kullanici.Id);
        var b = await Siparis.OlusturAsync(new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 2)]), Kullanici.Id);
        Assert.Equal("SP-0002", b.No);
        var kb = b.Kalemler[0].Id;
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.SeriAyirAsync(kb, new SeriAyirIstek(["SN-IP-0001"])));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.SeriAyirAsync(kb, new SeriAyirIstek(["SN-IP-0002", "SN-IP-0003", "SN-IP-0001"])));

        a = await Siparis.SeriBirakAsync(a.Kalemler[0].Id, "SN-IP-0001");
        Assert.Empty(a.Kalemler[0].Ayrilanlar);
        b = await Siparis.SeriAyirAsync(kb, new SeriAyirIstek(["SN-IP-0001", "SN-IP-0002"]));
        Assert.Equal("kargoya_verilecek", b.Durum);
        var bekleyen = Assert.Single(await new SevkiyatServisi(Db).ListeAsync(durum: "Hazirlaniyor"));
        Assert.Equal(b.Id, bekleyen.SiparisId);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002"], bekleyen.Kalemler.Select(x => x.SeriNo));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.SeriAyirAsync(kb, new SeriAyirIstek(["SN-IP-0003"])));
    }

    [Fact]
    public async Task Iptal_ayrilanlari_birakir_bekleyen_uretimi_kapatir()
    {
        var s = await Siparis.OlusturAsync(new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 4, ["SN-IP-0001"])]), Kullanici.Id);
        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, null, s.Kalemler[0].Id), Kullanici.Id);
        s = await Siparis.IptalAsync(s.Id, Kullanici.Id);
        Assert.Equal(("iptal", 0), (s.Durum, s.Kalemler[0].Ayrilanlar.Count));
        Assert.Equal("Iptal", (await Sira.GetirAsync(e.Id)).Durum);
        Assert.Equal(3, (await Siparis.HazirSerilerAsync(Urun.Id)).Count);
        Assert.Empty(await Siparis.ListeAsync(acik: true));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.IptalAsync(s.Id, Kullanici.Id));
    }

    [Fact]
    public async Task Gecersiz_siparisler()
    {
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.OlusturAsync(new SiparisIstek(Musteri.Id, []), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.OlusturAsync(
            new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 1), new SiparisKalemiIstek(Urun.Id, 2)]), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.OlusturAsync(
            new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 1, ["SN-IP-0001", "SN-IP-0002"])]), Kullanici.Id));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Siparis.OlusturAsync(
            new SiparisIstek(-1, [new SiparisKalemiIstek(Urun.Id, 1)]), Kullanici.Id));
    }
}

public class SevkiyatTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private ISiparisServisi Siparis = null!;
    private ISevkiyatServisi Sevkiyat = null!;
    private SiparisDto Sp = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Siparis = new SiparisServisi(Db);
        Sevkiyat = new SevkiyatServisi(Db);
        await Uretim.UretAsync(Urun.Id, 4, Kullanici.Id);
        Sp = await Siparis.OlusturAsync(new SiparisIstek(Musteri.Id,
            [new SiparisKalemiIstek(Urun.Id, 3, ["SN-IP-0001", "SN-IP-0002", "SN-IP-0003"])]), Kullanici.Id);
    }

    [Fact]
    public async Task Parca_parca_gonderilir_kargo_ve_teslim()
    {
        var s1 = Assert.Single(await Sevkiyat.ListeAsync(durum: "Hazirlaniyor"));
        Assert.Equal((null, "Hazirlaniyor", null, "Atlas"), (s1.No, s1.Durum, s1.SiparisNo, s1.MusteriAd));
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0003"], s1.Kalemler.Select(k => k.SeriNo));
        Assert.Equal("4500123", (await Siparis.SiparisNoAsync(Sp.Id, new SiparisNoIstek("  4500123  "))).SiparisNo);
        Assert.Equal("4500123", (await Sevkiyat.GetirAsync(s1.Id)).SiparisNo);
        Assert.Equal([s1.Id], (await Sevkiyat.ListeAsync("45001")).Select(x => x.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Siparis.SiparisNoAsync(Sp.Id, new SiparisNoIstek(new string('x', 51))));
        Assert.Equal("kargoya_verilecek", (await Siparis.GetirAsync(Sp.Id)).Durum);

        s1 = await Sevkiyat.KargoAsync(s1.Id, new KargoIstek("Yurtiçi Kargo", "123456", ["SN-IP-0001"]));
        Assert.Equal(((string?)null, "Kargoda", "Yurtiçi Kargo", "123456"), (s1.No, s1.Durum, s1.KargoFirmasi, s1.TakipNo));
        s1 = await Sevkiyat.NoAsync(s1.Id, new SevkiyatNoIstek("  KRG-1 "));
        Assert.Equal("KRG-1", s1.No);
        Assert.Equal("kismen_gonderildi", (await Siparis.GetirAsync(Sp.Id)).Durum);
        Assert.NotNull(s1.KargoZamani);
        s1 = await Sevkiyat.TeslimAsync(s1.Id);
        Assert.Equal(("KRG-1", "TeslimEdildi"), (s1.No, s1.Durum));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.TeslimAsync(s1.Id));
        var kalan = Assert.Single(await Sevkiyat.ListeAsync(durum: "Hazirlaniyor"));
        Assert.Equal(["SN-IP-0002", "SN-IP-0003"], kalan.Kalemler.Select(k => k.SeriNo));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.KargoAsync(kalan.Id, new KargoIstek("Aras Kargo", "999", ["SN-IP-0003"], "krg-1")));
        var s2 = await Sevkiyat.KargoAsync(kalan.Id, new KargoIstek("Aras Kargo", "999", ["SN-IP-0003"], "KRG-2"));
        Assert.Equal(("KRG-2", "Kargoda"), (s2.No, s2.Durum));
        Assert.Equal(["SN-IP-0003"], s2.Kalemler.Select(k => k.SeriNo));
        var s3 = Assert.Single(await Sevkiyat.ListeAsync(durum: "Hazirlaniyor"));
        Assert.Equal(["SN-IP-0002"], s3.Kalemler.Select(k => k.SeriNo));
        s3 = await Sevkiyat.TeslimAsync(s3.Id);
        Assert.Equal(((string?)null, "TeslimEdildi"), (s3.No, s3.Durum));
        Assert.Equal("tamamlandi", (await Siparis.GetirAsync(Sp.Id)).Durum);

        Assert.Equal(["KRG-2"], (await Sevkiyat.ListeAsync(durum: "Kargoda")).Select(s => s.No));
        Assert.Equal(["KRG-1"], (await Sevkiyat.ListeAsync("krg-1")).Select(s => s.No));
        Assert.Equal(["KRG-2"], (await Sevkiyat.ListeAsync("999")).Select(s => s.No));
    }

    [Fact]
    public async Task Ayrilmamis_ya_da_gonderilmis_urun_gonderilemez()
    {
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["SN-IP-0004"]), Kullanici.Id));
        var s = Assert.Single(await Sevkiyat.ListeAsync(durum: "Hazirlaniyor"));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["SN-IP-0001"]), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, []), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.KargoAsync(s.Id, new KargoIstek("", "")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.KargoAsync(s.Id, new KargoIstek("Aras Kargo", "1", ["SN-IP-0004"])));
    }
}

public class OzetTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private IUretimSirasiServisi Sira = null!;
    private IOzetServisi Ozet = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Sira = new UretimSirasiServisi(Db, new StokLotuRepository(Db), new UretimRepository(Db));
        Ozet = new OzetServisi(Db, Stok);
    }

    [Fact]
    public async Task Uc_ve_daha_az_uretilebilen_urun_uyarisi()
    {
        Assert.Empty((await Ozet.AnaSayfaAsync()).Uyarilar);
        await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 11), Kullanici.Id);
        var o = await Ozet.AnaSayfaAsync();
        var u = Assert.Single(o.Uyarilar);
        Assert.Equal(("IP", 2), (u.UrunKod, u.Uretilebilir));
        Assert.Equal([("DIY", 2)], u.Kisitlayanlar.Select(k => (k.Parca.Kod, k.Serbest)));
        Assert.Equal((1, 11), (o.SiradakiIs, o.SiradakiAdet));
        Assert.Equal(("İndikatör Paneli", 2), (Assert.Single(o.Kapasiteler).UrunAd, o.Kapasiteler[0].Uretilebilir));
        Assert.Empty(o.MinUyarilari);
        await Db.Parcalar.Where(p => p.Id == Diyot.Id).ExecuteUpdateAsync(s => s.SetProperty(p => p.MinStok, 2));
        o = await Ozet.AnaSayfaAsync();
        Assert.Equal(("DIY", 2), (Assert.Single(o.MinUyarilari).Kod, o.MinUyarilari[0].Serbest));
        Assert.Equal(1, o.MinAltindaParca);
    }

    [Fact]
    public async Task Takvim_gun_gun_planlanan_uretilen_istenen()
    {
        await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 2, PlanlananTarih: new(2026, 12, 1)), Kullanici.Id);
        await Uretim.UretAsync(Urun.Id, 3, Kullanici.Id, new DateTimeOffset(2026, 12, 2, 1, 0, 0, TimeSpan.FromHours(3)));
        var siparis = new SiparisServisi(Db);
        await siparis.OlusturAsync(new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 4)], IstenenTarih: new(2026, 12, 3)), Kullanici.Id);

        var t = await Ozet.TakvimAsync(new(2026, 12, 1), new(2026, 12, 31));
        Assert.Equal([new DateOnly(2026, 12, 1), new(2026, 12, 2), new(2026, 12, 3)], t.Select(g => g.Tarih));
        Assert.Equal((2, 0, 0), Tek(t[0]));
        Assert.Equal((0, 3, 0), Tek(t[1]));
        Assert.Equal((0, 0, 4), Tek(t[2]));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Ozet.TakvimAsync(new(2026, 1, 1), new(2026, 6, 1)));

        static (int, int, int) Tek(TakvimGunuDto g) { var u = Assert.Single(g.Urunler); return (u.Planlanan, u.Uretilen, u.Istenen); }
    }

    [Fact]
    public async Task Grafiklerde_haftalik_uretim_siparis_durumu_ve_gecikenler()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        var siparis = new SiparisServisi(Db);
        var gec = await siparis.OlusturAsync(new SiparisIstek(Musteri.Id, [new SiparisKalemiIstek(Urun.Id, 3)],
            Tarih: Zaman.Bugun().AddDays(-10), IstenenTarih: Zaman.Bugun().AddDays(-2)), Kullanici.Id);
        await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1, PlanlananTarih: Zaman.Bugun().AddDays(-1)), Kullanici.Id);

        var g = await Ozet.GrafiklerAsync();
        Assert.Equal(OzetServisi.HaftaSayisi, g.HaftalikUretim.Count);
        Assert.Equal(2, g.HaftalikUretim[^1].Adet);
        Assert.Equal(("bekliyor", 1), (Assert.Single(g.SiparisDurumlari).Durum, g.SiparisDurumlari[0].Sayi));
        Assert.Equal([("siparis", gec.MusteriAd, 3), ("uretim", "Stok için", 1)], g.Gecikenler.Select(x => (x.Tur, x.No, x.Kalan)));
        Assert.Equal((0, 2), (g.UrunDagilimi[0].Gonderilen, g.UrunDagilimi[0].Depoda));
    }

    [Fact]
    public async Task Takvime_parca_alimi_hatirlaticisi_eklenir_tamamlanir_silinir()
    {
        var p = await Ozet.PlanEkleAsync(new TakvimPlaniIstek(new(2026, 12, 5), Diyot.Id, 50, Ted.Id, "Fiyat sor"), Kullanici.Id);
        Assert.Equal(("DIY", 50, "Tedarikçi A", false), (p.Parca!.Kod, p.Adet, p.TedarikciAd, p.Tamamlandi));
        var not = await Ozet.PlanEkleAsync(new TakvimPlaniIstek(new(2026, 12, 5), Aciklama: "Bakım günü"), Kullanici.Id);
        Assert.Null(not.Parca);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Ozet.PlanEkleAsync(new TakvimPlaniIstek(new(2026, 12, 5)), Kullanici.Id));

        var gun = Assert.Single(await Ozet.TakvimAsync(new(2026, 12, 1), new(2026, 12, 31)));
        Assert.Equal((new DateOnly(2026, 12, 5), 2), (gun.Tarih, gun.Planlar.Count));
        Assert.Empty(gun.Urunler);
        Assert.True((await Ozet.PlanTamamAsync(p.Id, true)).Tamamlandi);
        await Ozet.PlanSilAsync(not.Id);
        Assert.Equal([p.Id], (await Ozet.TakvimAsync(new(2026, 12, 1), new(2026, 12, 31)))[0].Planlar.Select(x => x.Id));
        Assert.Equal(100 + 10 + 3 + 50, await Db.StokLotlari.SumAsync(l => l.KalanAdet));
    }

    [Fact]
    public async Task Excel_raporu_bolumleri_ayri_sayfalarda()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        var rapor = new RaporServisi(Db, Ozet, new SiparisServisi(Db), Uretim, new SevkiyatServisi(Db), Sira, Stok, new GeriCagirmaServisi(Db));
        var (icerik, ad) = await rapor.ExcelAsync(3);
        Assert.StartsWith("rapor-son-3-ay-", ad);
        using var wb = new ClosedXML.Excel.XLWorkbook(new MemoryStream(icerik));
        Assert.Equal(["Özet", "Talepler", "Üretim Sırası", "Üretim", "Sevkiyat", "Mal Girişi", "Parça Stoğu", "Geri Çağırmalar"],
                     wb.Worksheets.Select(w => w.Name));
        Assert.Equal("SN-IP-0002", wb.Worksheet("Üretim").Cell(2, 1).GetString());
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => rapor.ExcelAsync(2));
    }
}

public class LotSilmeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Kullanilmamis_lot_silinir_kullanilmis_silinmez()
    {
        await Assert.ThrowsAsync<YetkiHatasi>(() => Stok.LotSilAsync(DiyotYeni.Id, Kullanici.Id));
        await Stok.LotSilAsync(DiyotYeni.Id, Admin.Id);
        Assert.False(await Db.StokLotlari.AnyAsync(l => l.Id == DiyotYeni.Id));

        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Stok.LotSilAsync(LedLot.Id, Admin.Id));
        Assert.Contains("kullanıldı", hata.Message);
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Stok.LotSilAsync(-1, Admin.Id));
    }
}
