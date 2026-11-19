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
    public async Task Hazir_seri_ayrilir_eksik_siradan_uretilip_kaleme_gelir()
    {
        var s = await Siparis.OlusturAsync(new SiparisIstek(Musteri.Id,
            [new SiparisKalemiIstek(Urun.Id, 5, ["sn-ip-0001", "SN-IP-0002"])], IstenenTarih: new(2026, 12, 15)), Kullanici.Id);
        Assert.Equal(("SP-0001", "bekliyor"), (s.No, s.Durum));
        var k = Assert.Single(s.Kalemler);
        Assert.Equal((2, 0, 3), (k.Ayrilanlar.Count, k.Uretimde, k.Eksik));
        Assert.Equal(["SN-IP-0003"], (await Siparis.HazirSerilerAsync(Urun.Id)).Select(h => h.SeriNo));

        var e = await Sira.EkleAsync(new UretimEmriIstek(Urun.Id, null, k.Id), Kullanici.Id);
        Assert.Equal((3, "SP-0001", "Atlas"), (e.Adet, e.SiparisNo, e.MusteriAd));
        s = await Siparis.GetirAsync(s.Id);
        Assert.Equal(("uretimde", 0), (s.Durum, s.Kalemler[0].Eksik));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sira.EkleAsync(new UretimEmriIstek(Urun.Id, 1, k.Id), Kullanici.Id));

        await Sira.UretAsync(e.Id, new EmirUretIstek(), Kullanici.Id);
        s = await Siparis.GetirAsync(s.Id);
        Assert.Equal("hazir", s.Durum);
        Assert.Equal(["SN-IP-0001", "SN-IP-0002", "SN-IP-0004", "SN-IP-0005", "SN-IP-0006"], s.Kalemler[0].Ayrilanlar);
        Assert.Equal(["SN-IP-0003"], (await Siparis.HazirSerilerAsync(Urun.Id)).Select(h => h.SeriNo));

        await Assert.ThrowsAsync<SatisHatasi>(() => Satis.SatAsync(new SatisIstek("Beta", ["SN-IP-0004"]), Kullanici.Id));
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0004"]), Kullanici.Id);
        s = await Siparis.GetirAsync(s.Id);
        Assert.Equal("kismen_gonderildi", s.Durum);
        Assert.Equal(["SN-IP-0004"], s.Kalemler[0].Gonderilenler);
        Assert.Single(await Siparis.ListeAsync(Musteri.Id, acik: true));
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
        Assert.Equal("hazir", b.Durum);
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
        var s1 = await Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["SN-IP-0001"]), Kullanici.Id);
        Assert.Equal(("SV-0001", "Hazirlaniyor", "SP-0001", "Atlas"), (s1.No, s1.Durum, s1.SiparisNo, s1.MusteriAd));
        Assert.Equal("kismen_gonderildi", (await Siparis.GetirAsync(Sp.Id)).Durum);

        s1 = await Sevkiyat.KargoAsync(s1.Id, new KargoIstek("Yurtiçi Kargo", "123456"));
        Assert.Equal(("Kargoda", "Yurtiçi Kargo", "123456"), (s1.Durum, s1.KargoFirmasi, s1.TakipNo));
        Assert.NotNull(s1.KargoZamani);
        s1 = await Sevkiyat.TeslimAsync(s1.Id);
        Assert.Equal("TeslimEdildi", s1.Durum);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.TeslimAsync(s1.Id));

        var s2 = await Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["sn-ip-0002", "SN-IP-0003"], "Aras Kargo", "999"), Kullanici.Id);
        Assert.Equal(("SV-0002", "Kargoda"), (s2.No, s2.Durum));
        Assert.Equal(["SN-IP-0002", "SN-IP-0003"], s2.Kalemler.Select(k => k.SeriNo));
        Assert.Equal("tamamlandi", (await Siparis.GetirAsync(Sp.Id)).Durum);

        Assert.Equal(["SV-0002"], (await Sevkiyat.ListeAsync(durum: "Kargoda")).Select(s => s.No));
        Assert.Equal(["SV-0001"], (await Sevkiyat.ListeAsync("sv-0001")).Select(s => s.No));
        Assert.Equal(["SV-0002"], (await Sevkiyat.ListeAsync("999")).Select(s => s.No));
    }

    [Fact]
    public async Task Ayrilmamis_ya_da_gonderilmis_urun_gonderilemez()
    {
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["SN-IP-0004"]), Kullanici.Id));
        var s = await Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["SN-IP-0001"]), Kullanici.Id);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["SN-IP-0001"]), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, []), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.OlusturAsync(new SevkiyatIstek(Sp.Id, ["SN-IP-0002"], TakipNo: "1"), Kullanici.Id));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Sevkiyat.KargoAsync(s.Id, new KargoIstek("", "")));
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
        Assert.Equal(["Özet", "Siparişler", "Üretim Sırası", "Üretim", "Sevkiyat", "Mal Girişi", "Parça Stoğu", "Geri Çağırmalar"],
                     wb.Worksheets.Select(w => w.Name));
        Assert.Equal("SN-IP-0002", wb.Worksheet("Üretim").Cell(2, 1).GetString());
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => rapor.ExcelAsync(2));
    }
}
