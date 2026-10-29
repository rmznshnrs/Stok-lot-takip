using Microsoft.EntityFrameworkCore;

namespace Tests;

public class KullaniciSilmeTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private KullaniciServisi K => new(Db);

    private Task<KullaniciDto> Yeni(string ad, string rol = "Kullanici") =>
        K.OlusturAsync(new KullaniciOlusturIstek(ad, "Deneme", ad, "sifre1234", rol), Admin.Id);

    [Fact]
    public async Task Islemi_olmayan_kullanici_tamamen_silinir()
    {
        var k = await Yeni("ali");
        await Assert.ThrowsAsync<YetkiHatasi>(() => K.SilAsync(k.Id, Kullanici.Id));

        Assert.True((await K.SilAsync(k.Id, Admin.Id)).Silindi);
        Assert.False(await Db.Kullanicilar.AnyAsync(x => x.Id == k.Id));
    }

    [Fact]
    public async Task Islemi_olan_kullanici_pasif_olur_giris_ve_islem_yapamaz()
    {
        // Ortak verideki "depo" kullanıcısı lotları oluşturdu
        await Db.Kullanicilar.Where(x => x.Id == Kullanici.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.SifreHash, BCrypt.Net.BCrypt.HashPassword("sifre1234")));
        Assert.NotNull(await K.DogrulaAsync("depo", "sifre1234"));

        Assert.False((await K.SilAsync(Kullanici.Id, Admin.Id)).Silindi);
        Assert.True(await Db.Kullanicilar.AnyAsync(x => x.Id == Kullanici.Id && !x.Aktif));
        Assert.Null(await K.DogrulaAsync("depo", "sifre1234"));
        Assert.Null(await K.GetirAsync(Kullanici.Id));
        await Assert.ThrowsAsync<YetkiHatasi>(() => Uretim.UretAsync(Urun.Id, 1, Kullanici.Id));
        Assert.False((await K.ListeleAsync(Admin.Id)).Single(x => x.Id == Kullanici.Id).Aktif);

        Assert.True((await K.EtkinlestirAsync(Kullanici.Id, Admin.Id)).Aktif);
        Assert.NotNull(await K.DogrulaAsync("depo", "sifre1234"));
    }

    [Fact]
    public async Task Kendini_ve_son_admini_silemez()
    {
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => K.SilAsync(Admin.Id, Admin.Id));
        Assert.Contains("Kendi", hata.Message);

        var ikinci = await Yeni("ayse", "Admin");
        Assert.True((await K.SilAsync(ikinci.Id, Admin.Id)).Silindi);
        // Tek Admin kalınca başka bir Admin onu silemez, rolünü de düşüremez
        var ucuncu = await Yeni("veli", "Admin");
        await K.SilAsync(Admin.Id, ucuncu.Id);  // admin'in işlemi yok: silinir
        hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() =>
            K.GuncelleAsync(ucuncu.Id, new KullaniciGuncelleIstek("veli", "Deneme", "Kullanici"), ucuncu.Id));
        Assert.Contains("Son Admin", hata.Message);
    }
}

public class SatisZamaniTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    [Fact]
    public async Task Tarih_verilmezse_satis_saatiyle_kaydedilir()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        var once = Zaman.Simdi();
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id);
        var s = (await Satis.SatisGecmisiAsync()).Single();
        Assert.NotNull(s.Zaman);
        Assert.True(s.Zaman >= once.AddSeconds(-1));
        Assert.Equal(Zaman.YerelTarih(s.Zaman!.Value), s.Tarih);
    }

    [Fact]
    public async Task Verilen_saat_kullanilir_yalniz_tarih_verilirse_saat_bos_kalir()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        var an = new DateTimeOffset(2026, 10, 29, 14, 35, 0, TimeSpan.FromHours(3));
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"], Zaman: an), Kullanici.Id);
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0002"], new(2026, 5, 1)), Kullanici.Id);

        var gecmis = await Satis.SatisGecmisiAsync();
        var saatli = gecmis.Single(g => g.SeriNolar.Contains("SN-IP-0001"));
        Assert.Equal((new DateOnly(2026, 10, 29), (DateTimeOffset?)an), (saatli.Tarih, saatli.Zaman));
        var saatsiz = gecmis.Single(g => g.SeriNolar.Contains("SN-IP-0002"));
        Assert.Equal((new DateOnly(2026, 5, 1), (DateTimeOffset?)null), (saatsiz.Tarih, saatsiz.Zaman));
    }
}

public class GeriCagirmaAkisiTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private GeriCagirmaServisi Geri => new(Db);

    private async Task<int> Cagir(string seriNo) =>
        (await Geri.GeriCagirAsync(new([seriNo], "Kontrol edilecek"), Admin.Id)).Single().Id;

    [Fact]
    public async Task Musteriye_bilgi_verildi_isaretlenir()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id);
        var musteride = await Cagir("SN-IP-0001");
        var depoda = await Cagir("SN-IP-0002");

        await Assert.ThrowsAsync<YetkiHatasi>(() => Geri.BilgiVerildiAsync(musteride, Kullanici.Id));
        var g = await Geri.BilgiVerildiAsync(musteride, Admin.Id);
        Assert.Equal("musteride", g.Durum);
        Assert.NotNull(g.BilgiVerildiZamani);
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.BilgiVerildiAsync(depoda, Admin.Id));
        Assert.Contains("müşteride değil", hata.Message);
    }

    [Fact]
    public async Task Musterideki_urun_hurdaya_ayrilinca_once_iade_alinir()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        await Satis.SatAsync(new SatisIstek("Atlas", ["SN-IP-0001"]), Kullanici.Id);
        var id = await Cagir("SN-IP-0001");

        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.HurdayaAyirAsync(id, new(" "), Admin.Id));
        var g = await Geri.HurdayaAyirAsync(id, new("Onarılamaz"), Admin.Id);
        Assert.Equal(("hurda", "Atlas"), (g.Durum, g.MusteriAd));
        Assert.Empty(await Geri.ListeAsync(acik: true));
        var satis = (await Satis.SatisGecmisiAsync()).Single();
        Assert.Equal(["SN-IP-0001"], satis.IadeEdilenler);
        Assert.Equal("Onarılamaz", (await Db.UretimHurdalari.SingleAsync()).Aciklama);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Geri.HurdayaAyirAsync(id, new("tekrar"), Admin.Id));
    }

    [Fact]
    public async Task Depodaki_urun_hurdaya_ayrilir()
    {
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        var id = await Cagir("SN-IP-0001");
        Assert.Equal("hurda", (await Geri.HurdayaAyirAsync(id, new("Kırık"), Admin.Id)).Durum);
        Assert.Empty(await Db.SatisIadeleri.ToListAsync());
    }
}
