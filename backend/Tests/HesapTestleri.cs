using Microsoft.EntityFrameworkCore;

namespace Tests;

public class HesapTestleri(TestVeritabani vt) : IsKuraliTesti(vt)
{
    private sealed class SahtePosta : IPostaGonderici
    {
        public List<(string Alici, string Konu, string Metin)> Gidenler { get; } = [];

        public Task GonderAsync(string alici, string konu, string metin, CancellationToken ct = default)
        {
            Gidenler.Add((alici, konu, metin));
            return Task.CompletedTask;
        }
    }

    private readonly SahtePosta _posta = new();
    private HesapServisi Hesap => new(Db, _posta);

    private static KayitIstek Kayit(string kullaniciAdi = "ayse", string eposta = "ayse@ornek.com") =>
        new(" Ayşe ", "Yılmaz", kullaniciAdi, eposta, "gizli-sifre-1");

    private string Kod() => _posta.Gidenler[^1].Metin.Split("kod=")[1].Split('\n')[0].Trim();

    [Fact]
    public async Task Kayit_onay_bekler_yonetici_onaylayinca_giris_yapilir()
    {
        var k = await Hesap.KayitAsync(Kayit());
        Assert.Equal(("Ayşe", "ayse", "ayse@ornek.com"), (k.Ad, k.KullaniciAdi, k.Eposta));
        Assert.False(await Hesap.OnayliMiAsync(k.Id));
        Assert.Equal(KullaniciRolu.Kullanici, (await Db.Kullanicilar.SingleAsync(x => x.Id == k.Id)).Rol);
        Assert.True(await Hesap.OnayliMiAsync(Admin.Id));  // yönetici panelinden açılanlar onaylı

        await Assert.ThrowsAsync<YetkiHatasi>(() => Hesap.OnayBekleyenlerAsync(Kullanici.Id));
        Assert.Equal([k.Id], (await Hesap.OnayBekleyenlerAsync(Admin.Id)).Select(x => x.Id));
        await Assert.ThrowsAsync<YetkiHatasi>(() => Hesap.OnaylaAsync(k.Id, Kullanici.Id));
        await Hesap.OnaylaAsync(k.Id, Admin.Id);
        Assert.True(await Hesap.OnayliMiAsync(k.Id));
        Assert.Empty(await Hesap.OnayBekleyenlerAsync(Admin.Id));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Hesap.OnaylaAsync(k.Id, Admin.Id));
        await Assert.ThrowsAsync<BulunamadiHatasi>(() => Hesap.ReddetAsync(k.Id, Admin.Id));  // onaylı silinmez
    }

    [Fact]
    public async Task Reddedilen_kayit_silinir()
    {
        var k = await Hesap.KayitAsync(Kayit());
        await Hesap.ReddetAsync(k.Id, Admin.Id);
        Assert.False(await Db.Kullanicilar.AnyAsync(x => x.Id == k.Id));
        await Hesap.KayitAsync(Kayit());  // aynı ad yeniden alınabilir
    }

    [Theory]
    [InlineData("ayse", "x@ornek.com", "alınmış")]
    [InlineData("baska", "ayse@ornek.com", "kayıtlı")]
    [InlineData("baska", "gecersiz", "Geçerli bir e-posta")]
    [InlineData("baska", " ", "E-posta boş")]
    public async Task Kayit_hatalari(string kullaniciAdi, string eposta, string mesaj)
    {
        await Hesap.KayitAsync(Kayit());
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.KayitAsync(Kayit(kullaniciAdi, eposta)));
        Assert.Contains(mesaj, hata.Message);
    }

    [Fact]
    public async Task Kisa_sifreyle_kayit_olmaz() =>
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.KayitAsync(new("A", "B", "c", "c@ornek.com", "kisa")));

    [Fact]
    public async Task Sifremi_unuttum_epostayla_kod_gonderir_kod_bir_kez_kullanilir()
    {
        var k = await Hesap.KayitAsync(Kayit());
        await Hesap.SifreUnuttumAsync("ayse", "http://web.test/");
        Assert.Empty(_posta.Gidenler);  // onaysız hesaba gönderilmez

        await Hesap.OnaylaAsync(k.Id, Admin.Id);
        await Hesap.SifreUnuttumAsync("AYSE@ornek.com", "http://web.test/");
        var (alici, _, metin) = Assert.Single(_posta.Gidenler);
        Assert.Equal("ayse@ornek.com", alici);
        Assert.Contains("http://web.test/sifre-sifirla?kod=", metin);
        var kod = Kod();
        Assert.DoesNotContain(kod, await Db.SifreSifirlamalari.Select(s => s.KodOzeti).ToListAsync());  // kod açık saklanmaz

        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.SifreSifirlaAsync(new(kod, "kisa")));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.SifreSifirlaAsync(new("yanlis-kod", "yeni-sifre-12")));
        await Hesap.SifreSifirlaAsync(new(kod, "yeni-sifre-12"));
        Db.ChangeTracker.Clear();
        Assert.NotNull(await new KullaniciServisi(Db).DogrulaAsync("ayse", "yeni-sifre-12"));
        Assert.Null(await new KullaniciServisi(Db).DogrulaAsync("ayse", "gizli-sifre-1"));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.SifreSifirlaAsync(new(kod, "baska-sifre-12")));
    }

    [Fact]
    public async Task Bilinmeyen_hesap_ve_epostasiz_hesap_icin_hata_vermez_posta_gitmez()
    {
        await Hesap.SifreUnuttumAsync("yok", "http://web.test");
        await Hesap.SifreUnuttumAsync(Kullanici.KullaniciAdi, "http://web.test");  // e-postası yok
        Assert.Empty(_posta.Gidenler);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.SifreUnuttumAsync(" ", "http://web.test"));
    }

    [Fact]
    public async Task Suresi_dolan_kod_gecmez()
    {
        await Hesap.EpostaAyarlaAsync(Kullanici.Id, "depo@ornek.com");
        await Hesap.SifreUnuttumAsync("depo", "http://web.test");
        await Db.SifreSifirlamalari.ExecuteUpdateAsync(s => s.SetProperty(x => x.SonGecerlilik, Zaman.Simdi().AddMinutes(-1)));
        Db.ChangeTracker.Clear();
        var hata = await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.SifreSifirlaAsync(new(Kod(), "yeni-sifre-12")));
        Assert.Contains("süresi dolmuş", hata.Message);
    }

    [Fact]
    public async Task Eposta_ayarlanir_kaldirilir_tekil_olur()
    {
        Assert.Null((await Hesap.EpostaGetirAsync(Kullanici.Id)).Eposta);
        Assert.Equal("depo@ornek.com", (await Hesap.EpostaAyarlaAsync(Kullanici.Id, " depo@ornek.com ")).Eposta);
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.EpostaAyarlaAsync(Admin.Id, "depo@ornek.com"));
        await Assert.ThrowsAsync<IsKuraliHatasi>(() => Hesap.EpostaAyarlaAsync(Admin.Id, "olmaz"));
        Assert.Null((await Hesap.EpostaAyarlaAsync(Kullanici.Id, "")).Eposta);
        Assert.Equal("depo@ornek.com", (await Hesap.EpostaAyarlaAsync(Admin.Id, "depo@ornek.com")).Eposta);
    }
}
