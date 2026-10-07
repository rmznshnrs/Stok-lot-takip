using LotTakip.Business;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.EntityFrameworkCore;

namespace LotTakip.Tests;

public class TanimTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private ITanimServisi Tanim = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Tanim = new TanimServisi(Db, Stok);
    }

    [Fact]
    public async Task Parca_olustur_ve_guncelle()
    {
        var p = await Tanim.ParcaOlusturAsync(new ParcaIstek(" STM32F103 ", " Mikro  denetleyici ", "", 5));
        Assert.Equal(("STM32F103", "Mikro denetleyici", "adet"), (p.Kod, p.Ad, p.Birim));
        p = await Tanim.ParcaGuncelleAsync(p.Id, new ParcaIstek("STM32F103", "MCU", "adet", 10));
        Assert.Equal("MCU", p.Ad);
        Assert.Equal(10, await Db.Parcalar.Where(x => x.Id == p.Id).Select(x => x.MinStok).SingleAsync());
    }

    [Fact]
    public async Task Parca_kodu_buyuk_kucuk_harf_fark_etmeden_benzersiz()
    {
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Tanim.ParcaOlusturAsync(new ParcaIstek("diy", "X")));
        Assert.Equal("Bu kodla bir parça zaten var.", hata.Message);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Tanim.ParcaGuncelleAsync(Led.Id, new ParcaIstek("DIY", "LED")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Tanim.ParcaOlusturAsync(new ParcaIstek("X", "Y", MinStok: -1)));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Tanim.ParcaGuncelleAsync(-1, new ParcaIstek("X", "Y")));
    }

    [Fact]
    public async Task Urun_listesi_ve_detay()
    {
        await Uretim.UretAsync(Urun.Id, 2, Kullanici.Id);
        var liste = await Tanim.UrunleriListeleAsync();
        Assert.Equal(new UrunListeDto(Urun.Id, "IP", "İndikatör Paneli", "SN-IP-", 2, 2), Assert.Single(liste));
        Assert.Empty(await Tanim.UrunleriListeleAsync("zzz"));

        var d = (await Tanim.UrunGetirAsync(Urun.Id))!;
        Assert.False(d.SeriOnekiDegistirilebilir);  // üretim var
        Assert.Equal([("DIY", 1, 11), ("LED", 4, 92)], d.Agac.Select(a => (a.ParcaKod, a.Adet, a.KullanilabilirStok)));
        Assert.Null(await Tanim.UrunGetirAsync(-1));
    }

    [Fact]
    public async Task Urun_olustur_ve_agac_kaydet()
    {
        var u = await Tanim.UrunOlusturAsync(new UrunIstek("RK", "Röle Kartı", "SN-RK-"));
        Assert.True(u.SeriOnekiDegistirilebilir);
        Assert.Empty(u.Agac);

        u = await Tanim.UrunAgaciKaydetAsync(u.Id, [new(Diyot.Id, 2), new(Led.Id, 1)]);
        Assert.Equal([("DIY", 2), ("LED", 1)], u.Agac.Select(a => (a.ParcaKod, a.Adet)));

        // Adet değiştir, satır kaldır: LED gider, DIY 3 olur
        u = await Tanim.UrunAgaciKaydetAsync(u.Id, [new(Diyot.Id, 3)]);
        Assert.Equal([("DIY", 3)], u.Agac.Select(a => (a.ParcaKod, a.Adet)));
    }

    [Fact]
    public async Task Agac_hatalari_hicbir_sey_degistirmez()
    {
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Tanim.UrunAgaciKaydetAsync(Urun.Id, [new(Led.Id, 0)]));
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(
            () => Tanim.UrunAgaciKaydetAsync(Urun.Id, [new(Led.Id, 1), new(Led.Id, 2)]));
        Assert.Contains("LED ağaçta birden fazla kez var", hata.Message);
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Tanim.UrunAgaciKaydetAsync(Urun.Id, [new(-1, 1)]));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Tanim.UrunAgaciKaydetAsync(-1, []));
        Assert.Equal([("DIY", 1), ("LED", 4)],
            (await Tanim.UrunGetirAsync(Urun.Id))!.Agac.Select(a => (a.ParcaKod, a.Adet)));
    }

    [Fact]
    public async Task Urun_benzersizlik_ve_onek_kurali()
    {
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Tanim.UrunOlusturAsync(new UrunIstek("ip", "X", "SN-X-")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Tanim.UrunOlusturAsync(new UrunIstek("X", "X", "sn-ip-")));

        // Üretim yokken önek değişir, üretim varsa değişmez ama ad değişebilir
        var u = await Tanim.UrunGuncelleAsync(Urun.Id, new UrunIstek("IP", "Panel", "SN-PNL-"));
        Assert.Equal("SN-PNL-", u.SeriOneki);
        await Uretim.UretAsync(Urun.Id, 1, Kullanici.Id);
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(
            () => Tanim.UrunGuncelleAsync(Urun.Id, new UrunIstek("IP", "Panel v2", "SN-YENI-")));
        Assert.Contains("seri no öneki değiştirilemez", hata.Message);
        u = await Tanim.UrunGuncelleAsync(Urun.Id, new UrunIstek("IP", "Panel v2", "SN-PNL-"));
        Assert.Equal(("Panel v2", "SN-PNL-"), (u.Ad, u.SeriOneki));
    }

    [Fact]
    public async Task Tedarikci_ve_musteri_listeleri()
    {
        Assert.Equal(["Tedarikçi A"], (await Tanim.TedarikcilerAsync()).Select(t => t.Ad));
        Assert.Equal(["Atlas"], (await Tanim.MusterilerAsync()).Select(m => m.Ad));
    }
}

public class KullaniciTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private IKullaniciServisi K = null!;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        K = new KullaniciServisi(Db);
    }

    [Fact]
    public async Task Olustur_ve_dogrula()
    {
        var yeni = await K.OlusturAsync(new KullaniciOlusturIstek("Ayşe", "Yılmaz", "ayse", "gizli-sifre-1"), Admin.Id);
        Assert.Equal(("ayse", "Kullanici"), (yeni.KullaniciAdi, yeni.Rol));
        var hash = await Db.Kullanicilar.Where(k => k.Id == yeni.Id).Select(k => k.SifreHash).SingleAsync();
        Assert.NotEqual("gizli-sifre-1", hash);  // şifre düz metin saklanmaz

        Assert.Equal(yeni.Id, (await K.DogrulaAsync(" AYSE ", "gizli-sifre-1"))!.Id);  // kullanıcı adı harf duyarsız
        Assert.Null(await K.DogrulaAsync("ayse", "yanlis-sifre"));
        Assert.Null(await K.DogrulaAsync("olmayan", "gizli-sifre-1"));
    }

    [Theory]
    [InlineData("ayse", "kisa", "Admin")]          // şifre kısa
    [InlineData("ay se", "gizli-sifre-1", "Kullanici")]  // boşluklu ad
    [InlineData("depo", "gizli-sifre-1", "Kullanici")]   // kullanıcı adı var
    [InlineData("ayse", "gizli-sifre-1", "Patron")]      // rol yok
    public async Task Olusturma_hatalari(string kullaniciAdi, string sifre, string rol) =>
        await Assert.ThrowsAsync<IsKuraliHatasi>(
            () => K.OlusturAsync(new KullaniciOlusturIstek("A", "B", kullaniciAdi, sifre, rol), Admin.Id));

    [Fact]
    public async Task Yonetim_yalniz_admin()
    {
        await Assert.ThrowsAsync<YetkiHatasi>(() => K.ListeleAsync(Kullanici.Id));
        await Assert.ThrowsAsync<YetkiHatasi>(
            () => K.OlusturAsync(new KullaniciOlusturIstek("A", "B", "yeni", "gizli-sifre-1"), Kullanici.Id));
        await Assert.ThrowsAsync<YetkiHatasi>(
            () => K.GuncelleAsync(Kullanici.Id, new KullaniciGuncelleIstek("A", "B", "Admin"), Kullanici.Id));
        Assert.Equal(["depo", "yonetici"], (await K.ListeleAsync(Admin.Id)).Select(k => k.KullaniciAdi));
    }

    [Fact]
    public async Task Guncelle_rol_ve_sifre()
    {
        var k = await K.GuncelleAsync(Kullanici.Id, new KullaniciGuncelleIstek("Depo", "Sorumlusu", "Admin", "yeni-sifre-12"), Admin.Id);
        Assert.Equal(("Sorumlusu", "Admin"), (k.Soyad, k.Rol));
        Assert.NotNull(await K.DogrulaAsync("depo", "yeni-sifre-12"));
        // Şifre boş bırakılırsa değişmez
        await K.GuncelleAsync(Kullanici.Id, new KullaniciGuncelleIstek("Depo", "Sorumlusu", "Admin"), Admin.Id);
        Assert.NotNull(await K.DogrulaAsync("depo", "yeni-sifre-12"));
    }

    [Fact]
    public async Task Son_adminin_yetkisi_alinamaz()
    {
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(
            () => K.GuncelleAsync(Admin.Id, new KullaniciGuncelleIstek("Ad", "Min", "Kullanici"), Admin.Id));
        Assert.Contains("Son Admin", hata.Message);
        // Başka bir Admin varsa alınabilir
        await K.GuncelleAsync(Kullanici.Id, new KullaniciGuncelleIstek("Depo", "G", "Admin"), Admin.Id);
        Assert.Equal("Kullanici", (await K.GuncelleAsync(Admin.Id, new KullaniciGuncelleIstek("Ad", "Min", "Kullanici"), Admin.Id)).Rol);
    }

    [Fact]
    public async Task Kendi_sifresini_degistirir()
    {
        await Db.Kullanicilar.Where(k => k.Id == Kullanici.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.SifreHash, BCrypt.Net.BCrypt.HashPassword("eski-sifre-1")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => K.SifreDegistirAsync(Kullanici.Id, new("yanlis", "yeni-sifre-1")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => K.SifreDegistirAsync(Kullanici.Id, new("eski-sifre-1", "kisa")));
        await K.SifreDegistirAsync(Kullanici.Id, new("eski-sifre-1", "yeni-sifre-1"));
        Assert.NotNull(await K.DogrulaAsync("depo", "yeni-sifre-1"));
        Assert.Null(await K.DogrulaAsync("depo", "eski-sifre-1"));
        Assert.Equal(KullaniciRolu.Kullanici.ToString(), (await K.GetirAsync(Kullanici.Id))!.Rol);
    }
}
