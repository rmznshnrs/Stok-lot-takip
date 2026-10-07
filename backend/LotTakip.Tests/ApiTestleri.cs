using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LotTakip.Business;
using LotTakip.Entity;
using LotTakip.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace LotTakip.Tests;

/// <summary>API'yi test veritabanı ve test JWT anahtarıyla bellekte ayağa kaldırır.</summary>
public sealed class ApiFabrikasi : WebApplicationFactory<Program>
{
    public const string JwtAnahtar = "test-icin-en-az-otuz-iki-karakterlik-jwt-anahtari";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");  // Swagger açık
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:LotTakip"] = TestVeritabani.Baglanti,
            ["Jwt:Anahtar"] = JwtAnahtar,
            ["Jwt:Yayinci"] = "LotTakip",
            ["Jwt:Hedef"] = "LotTakip",
        }));
    }
}

/// <summary>
/// API testleri. API her istekte kendi bağlamını kullandığı için veri commit edilir; her test
/// başında veritabanı temizlenip örnek veri yüklenir. Kullanıcılar: admin (Admin), depo (Kullanici).
/// </summary>
[Collection(VeritabaniKoleksiyonu.Ad)]
public class ApiTestleri(TestVeritabani vt) : IAsyncLifetime
{
    private const string DepoSifre = "depo-sifresi-1";
    private ApiFabrikasi _fabrika = null!;
    private int _adminId, _depoId;

    public async Task InitializeAsync()
    {
        await vt.TumVeriyiSilAsync();
        await using (var db = vt.YeniContext())
        {
            await OrnekVeri.Olustur(db).YukleAsync(TestVeritabani.AdminSifre, sifirla: false);
            var depo = new Kullanici
            {
                Ad = "Depo", Soyad = "Görevlisi", KullaniciAdi = "depo", Rol = KullaniciRolu.Kullanici,
                SifreHash = BCrypt.Net.BCrypt.HashPassword(DepoSifre),
            };
            db.Kullanicilar.Add(depo);
            await db.SaveChangesAsync();
            _depoId = depo.Id;
            _adminId = await db.Kullanicilar.Where(k => k.KullaniciAdi == "admin").Select(k => k.Id).SingleAsync();
        }
        _fabrika = new ApiFabrikasi();
    }

    public async Task DisposeAsync()
    {
        await _fabrika.DisposeAsync();
        await vt.TumVeriyiSilAsync();
    }

    private HttpClient Istemci(string? token = null)
    {
        var c = _fabrika.CreateClient();
        if (token is not null)
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return c;
    }

    private async Task<GirisSonucuDto> GirisYap(string kullaniciAdi, string sifre)
    {
        var y = await Istemci().PostAsJsonAsync("/api/kimlik/giris", new GirisIstek(kullaniciAdi, sifre));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        return (await y.Content.ReadFromJsonAsync<GirisSonucuDto>())!;
    }

    private async Task<HttpClient> Admin() => Istemci((await GirisYap("admin", TestVeritabani.AdminSifre)).Token);
    private async Task<HttpClient> Depo() => Istemci((await GirisYap("depo", DepoSifre)).Token);

    private static async Task<JsonElement> Problem(HttpResponseMessage y) =>
        JsonDocument.Parse(await y.Content.ReadAsStringAsync()).RootElement;

    // --- Giriş zorunluluğu ----------------------------------------------------------

    public static TheoryData<string, string> KorunanUclar => new()
    {
        { "GET", "/api/kimlik/ben" }, { "POST", "/api/kimlik/sifre" },
        { "GET", "/api/kullanicilar" }, { "POST", "/api/kullanicilar" }, { "PUT", "/api/kullanicilar/1" },
        { "GET", "/api/parcalar" }, { "GET", "/api/parcalar/bul?metin=LED" }, { "POST", "/api/parcalar" }, { "PUT", "/api/parcalar/1" },
        { "GET", "/api/urunler" }, { "GET", "/api/urunler/1" }, { "POST", "/api/urunler" }, { "PUT", "/api/urunler/1" }, { "PUT", "/api/urunler/1/agac" },
        { "GET", "/api/tedarikciler" }, { "GET", "/api/musteriler" },
        { "GET", "/api/stok" }, { "POST", "/api/stok/giris" }, { "POST", "/api/stok/duzeltme" },
        { "GET", "/api/uretim/onizleme?urunId=1&adet=1" }, { "POST", "/api/uretim" },
        { "GET", "/api/satis/kontrol?seriNo=SN-IP-0003" }, { "POST", "/api/satis/uyarilar" }, { "POST", "/api/satis" }, { "GET", "/api/satis/gecmis" },
        { "GET", "/api/izleme/lot/L-D-2601" }, { "GET", "/api/izleme/seri/SN-IP-0001" }, { "GET", "/api/izleme/ara?q=L" },
        { "GET", "/api/izleme/hareketler" }, { "POST", "/api/izleme/lot/L-D-2601/geri-cagir" },
    };

    [Theory]
    [MemberData(nameof(KorunanUclar))]
    public async Task Giris_yapilmadan_tum_uclar_401(string yontem, string adres)
    {
        var y = await Istemci().SendAsync(new HttpRequestMessage(new HttpMethod(yontem), adres)
        {
            Content = yontem == "GET" ? null : new StringContent("{}", Encoding.UTF8, "application/json"),
        });
        Assert.Equal(HttpStatusCode.Unauthorized, y.StatusCode);
    }

    [Fact]
    public async Task Saglik_ve_giris_acik()
    {
        Assert.Equal(HttpStatusCode.OK, (await Istemci().GetAsync("/api/saglik")).StatusCode);
        // Giriş ucuna girişsiz ulaşılır: yanlış şifredeki 401 uç'un kendi yanıtıdır (başlığı "Giriş başarısız")
        var y = await Istemci().PostAsJsonAsync("/api/kimlik/giris", new GirisIstek("x", "y"));
        Assert.Equal("Giriş başarısız", (await Problem(y)).GetProperty("title").GetString());
    }

    [Fact]
    public async Task Sahte_imzali_ve_suresi_dolmus_token_reddedilir()
    {
        string Token(string anahtar, DateTime bitis) => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = "LotTakip", Audience = "LotTakip", NotBefore = bitis.AddHours(-9), IssuedAt = bitis.AddHours(-9), Expires = bitis,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(anahtar)), SecurityAlgorithms.HmacSha256),
            Claims = new Dictionary<string, object> { ["sub"] = _adminId.ToString(), ["role"] = "Admin" },
        });
        var sahte = Token("baska-bir-anahtar-otuz-iki-karakterden-uzun", DateTime.UtcNow.AddHours(1));
        var eski = Token(ApiFabrikasi.JwtAnahtar, DateTime.UtcNow.AddMinutes(-5));
        var gecerli = Token(ApiFabrikasi.JwtAnahtar, DateTime.UtcNow.AddHours(1));
        Assert.Equal(HttpStatusCode.Unauthorized, (await Istemci(sahte).GetAsync("/api/parcalar")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Istemci(eski).GetAsync("/api/parcalar")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Istemci(gecerli).GetAsync("/api/parcalar")).StatusCode);
    }

    // --- Giriş ------------------------------------------------------------------------

    [Fact]
    public async Task Giris_token_ve_kullanici_doner()
    {
        var g = await GirisYap("ADMIN", TestVeritabani.AdminSifre);  // kullanıcı adı harf duyarsız
        Assert.Equal(("admin", Roller.Admin), (g.Kullanici.KullaniciAdi, g.Kullanici.Rol));
        Assert.InRange(g.GecerlilikSonu, DateTimeOffset.UtcNow.AddHours(7.9), DateTimeOffset.UtcNow.AddHours(8.1));
        var ben = await Istemci(g.Token).GetFromJsonAsync<KullaniciDto>("/api/kimlik/ben");
        Assert.Equal(_adminId, ben!.Id);
        Assert.Contains("\"role\":\"Admin\"", Encoding.UTF8.GetString(Base64UrlEncoder.DecodeBytes(g.Token.Split('.')[1])));
    }

    [Theory]
    [InlineData("admin", "yanlis-sifre")]
    [InlineData("olmayan", "herhangi")]
    public async Task Hatali_giris_401_ve_ayni_mesaj(string kullaniciAdi, string sifre)
    {
        var y = await Istemci().PostAsJsonAsync("/api/kimlik/giris", new GirisIstek(kullaniciAdi, sifre));
        Assert.Equal(HttpStatusCode.Unauthorized, y.StatusCode);
        Assert.Equal("Kullanıcı adı veya şifre hatalı.", (await Problem(y)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Eksik_alanli_istek_400()
    {
        var y = await Istemci().PostAsJsonAsync("/api/kimlik/giris", new { kullaniciAdi = "admin" });
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
    }

    [Fact]
    public async Task Kendi_sifresini_degistirir()
    {
        var c = await Depo();
        var y = await c.PostAsJsonAsync("/api/kimlik/sifre", new SifreDegistirIstek("yanlis", "yeni-sifre-12"));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Equal("Mevcut şifre hatalı.", (await Problem(y)).GetProperty("detail").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await c.PostAsJsonAsync("/api/kimlik/sifre", new SifreDegistirIstek(DepoSifre, "yeni-sifre-12"))).StatusCode);
        await GirisYap("depo", "yeni-sifre-12");
    }

    // --- Roller -----------------------------------------------------------------------

    [Fact]
    public async Task Admin_islemleri_kullaniciya_403()
    {
        var depo = await Depo();
        var lotId = await LotId("L-D-2601");
        Assert.Equal(HttpStatusCode.Forbidden, (await depo.PostAsJsonAsync("/api/izleme/lot/L-D-2601/geri-cagir", new GeriCagirIstek())).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await depo.PostAsJsonAsync("/api/stok/duzeltme", new StokDuzeltmeIstek(lotId, -1, "Fire", "x"))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await depo.GetAsync("/api/kullanicilar")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await depo.PostAsJsonAsync("/api/kullanicilar", new KullaniciOlusturIstek("A", "B", "x", "sifre-12345"))).StatusCode);
        await using var db = vt.YeniContext();
        Assert.False(await db.StokLotlari.Where(l => l.Id == lotId).Select(l => l.GeriCagrildi).SingleAsync());
        Assert.False(await db.StokDuzeltmeleri.AnyAsync());
    }

    [Fact]
    public async Task Admin_geri_cagirir_ve_stok_duzeltir()
    {
        var admin = await Admin();
        var y = await admin.PostAsJsonAsync("/api/izleme/lot/l-d-2601/geri-cagir", new GeriCagirIstek());
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        Assert.True((await y.Content.ReadFromJsonAsync<LotDto>())!.GeriCagrildi);

        y = await admin.PostAsJsonAsync("/api/stok/duzeltme", new StokDuzeltmeIstek(await LotId("L-D-2603"), -2, "Fire", "kırık"));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        Assert.Equal(48, (await y.Content.ReadFromJsonAsync<StokDuzeltmeSonucuDto>())!.YeniKalanAdet);
        await using var db = vt.YeniContext();
        Assert.Equal(_adminId, await db.StokDuzeltmeleri.Select(d => d.OlusturanKullaniciId).SingleAsync());
    }

    [Fact]
    public async Task Kullanici_yonetimi()
    {
        var admin = await Admin();
        var y = await admin.PostAsJsonAsync("/api/kullanicilar", new KullaniciOlusturIstek("Ayşe", "Yılmaz", "ayse", "ayse-sifre-1"));
        Assert.Equal(HttpStatusCode.Created, y.StatusCode);
        var ayse = (await y.Content.ReadFromJsonAsync<KullaniciDto>())!;
        Assert.Equal(Roller.Kullanici, (await GirisYap("ayse", "ayse-sifre-1")).Kullanici.Rol);

        y = await admin.PutAsJsonAsync($"/api/kullanicilar/{ayse.Id}", new KullaniciGuncelleIstek("Ayşe", "Yılmaz", Roller.Admin));
        Assert.Equal(Roller.Admin, (await y.Content.ReadFromJsonAsync<KullaniciDto>())!.Rol);
        Assert.Equal(3, (await admin.GetFromJsonAsync<List<KullaniciDto>>("/api/kullanicilar"))!.Count);

        // Son Admin kuralı: ayşe tekrar Kullanici, sonra admin kendini düşürmeye çalışır
        await admin.PutAsJsonAsync($"/api/kullanicilar/{ayse.Id}", new KullaniciGuncelleIstek("Ayşe", "Yılmaz", Roller.Kullanici));
        y = await admin.PutAsJsonAsync($"/api/kullanicilar/{_adminId}", new KullaniciGuncelleIstek("Sistem", "Yöneticisi", Roller.Kullanici));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Contains("Son Admin", (await Problem(y)).GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Rolu_dusurulen_kullanicinin_eski_tokeni_admin_islemi_yapamaz()
    {
        // Token'da hâlâ "Admin" yazsa da Business veritabanındaki rolü denetler
        var admin = await Admin();
        var y = await admin.PostAsJsonAsync("/api/kullanicilar", new KullaniciOlusturIstek("Geçici", "Admin", "gecici", "gecici-sifre-1", Roller.Admin));
        var gecici = (await y.Content.ReadFromJsonAsync<KullaniciDto>())!;
        var geciciIstemci = Istemci((await GirisYap("gecici", "gecici-sifre-1")).Token);
        await admin.PutAsJsonAsync($"/api/kullanicilar/{gecici.Id}", new KullaniciGuncelleIstek("Geçici", "Admin", Roller.Kullanici));

        y = await geciciIstemci.PostAsJsonAsync("/api/izleme/lot/L-D-2601/geri-cagir", new GeriCagirIstek());
        Assert.Equal(HttpStatusCode.Forbidden, y.StatusCode);
    }

    // --- İşlemi yapan kullanıcı ---------------------------------------------------------

    [Fact]
    public async Task Islemi_yapan_kullanici_tokendan_alinip_kayitlara_yazilir()
    {
        var depo = await Depo();
        var parcaId = (await depo.GetFromJsonAsync<ParcaOzetDto>("/api/parcalar/bul?metin=diyot"))!.Id;
        var y = await depo.PostAsJsonAsync("/api/stok/giris", new MalGirisiIstek(parcaId, "Mikro Devre Ltd.", "L-D-API", 20, new(2026, 9, 1)));
        Assert.Equal(HttpStatusCode.Created, y.StatusCode);

        var urunId = (await depo.GetFromJsonAsync<List<UrunListeDto>>("/api/urunler"))!.Single(u => u.Kod == "IP").Id;
        y = await depo.PostAsJsonAsync("/api/uretim", new UretimIstek(urunId, 1));
        Assert.Equal(HttpStatusCode.OK, y.StatusCode);
        var seri = (await y.Content.ReadFromJsonAsync<UretimSonucuDto>())!.SeriNolar.Single();
        Assert.Equal("SN-IP-0007", seri);

        y = await depo.PostAsJsonAsync("/api/satis", new SatisIstek("Atlas Otomasyon", [seri]));
        Assert.Equal(HttpStatusCode.Created, y.StatusCode);

        await using var db = vt.YeniContext();
        Assert.Equal(_depoId, await db.StokLotlari.Where(l => l.LotNo == "L-D-API").Select(l => l.OlusturanKullaniciId).SingleAsync());
        Assert.Equal(_depoId, await db.Uretimler.Where(u => u.SeriNo == seri).Select(u => u.OlusturanKullaniciId).SingleAsync());
        Assert.Equal(_depoId, await db.Satislar.Where(s => s.Kalemler.Any(k => k.Uretim!.SeriNo == seri)).Select(s => s.OlusturanKullaniciId).SingleAsync());
    }

    // --- Hata yanıtları ---------------------------------------------------------------

    [Fact]
    public async Task Yetersiz_stok_409_ve_eksikler()
    {
        var c = await Depo();
        var urunId = (await c.GetFromJsonAsync<List<UrunListeDto>>("/api/urunler"))!.Single(u => u.Kod == "IP").Id;
        var o = (await c.GetFromJsonAsync<UretimOnizlemeDto>($"/api/uretim/onizleme?urunId={urunId}&adet=50"))!;
        Assert.False(o.Yeterli);
        Assert.Equal(["SN-IP-0007", "SN-IP-0056"], new[] { o.SeriNolar[0], o.SeriNolar[^1] });

        var y = await c.PostAsJsonAsync("/api/uretim", new UretimIstek(urunId, 50));
        Assert.Equal(HttpStatusCode.Conflict, y.StatusCode);
        var p = await Problem(y);
        Assert.Equal("Yetersiz stok", p.GetProperty("title").GetString());
        var eksikler = p.GetProperty("eksikler").EnumerateArray().Select(e => e.GetProperty("parca").GetProperty("kod").GetString()).ToList();
        Assert.Contains("LED-KR", eksikler);
        await using var db = vt.YeniContext();
        Assert.Equal(9, await db.Uretimler.CountAsync());  // hiçbir şey kaydedilmedi
    }

    [Fact]
    public async Task Geri_cagrilan_lotta_satis_onay_ister()
    {
        await (await Admin()).PostAsJsonAsync("/api/izleme/lot/L-D-2601/geri-cagir", new GeriCagirIstek());
        var c = await Depo();
        var kontrol = (await c.GetFromJsonAsync<SatilabilirDto>("/api/satis/kontrol?seriNo=sn-ip-0003"))!;
        Assert.Equal(["L-D-2601"], kontrol.GeriCagrilanLotlar);

        var y = await c.PostAsJsonAsync("/api/satis", new SatisIstek("Beta Enerji", ["SN-IP-0003"]));
        Assert.Equal(HttpStatusCode.Conflict, y.StatusCode);
        Assert.Equal("SN-IP-0003", (await Problem(y)).GetProperty("uyarilar")[0].GetProperty("seriNo").GetString());

        y = await c.PostAsJsonAsync("/api/satis", new SatisIstek("Beta Enerji", ["SN-IP-0003"], GeriCagrilanOnayi: true));
        Assert.Equal(HttpStatusCode.Created, y.StatusCode);
        Assert.Single((await y.Content.ReadFromJsonAsync<SatisSonucuDto>())!.Uyarilar);
    }

    [Fact]
    public async Task Satilmis_seri_400_bilinmeyenler_404()
    {
        var c = await Depo();
        var y = await c.PostAsJsonAsync("/api/satis", new SatisIstek("Beta Enerji", ["SN-IP-0001"]));
        Assert.Equal(HttpStatusCode.BadRequest, y.StatusCode);
        Assert.Contains("daha önce satılmış: Atlas Otomasyon", (await Problem(y)).GetProperty("detail").GetString());

        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/izleme/lot/YOK")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/izleme/seri/YOK")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/urunler/99999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync("/api/uretim/onizleme?urunId=99999&adet=1")).StatusCode);
    }

    // --- Uçtan uca ----------------------------------------------------------------------

    [Fact]
    public async Task Tanimla_agac_kur_uret_sat_izle()
    {
        var c = await Depo();
        var y = await c.PostAsJsonAsync("/api/parcalar", new ParcaIstek("KASA-1", "Kasa", MinStok: 2));
        Assert.Equal(HttpStatusCode.Created, y.StatusCode);
        var kasa = (await y.Content.ReadFromJsonAsync<ParcaOzetDto>())!;
        Assert.Equal(HttpStatusCode.BadRequest, (await c.PostAsJsonAsync("/api/parcalar", new ParcaIstek("kasa-1", "Kopya"))).StatusCode);

        y = await c.PostAsJsonAsync("/api/urunler", new UrunIstek("KT", "Kontrol Kutusu", "SN-KT-"));
        var urun = (await y.Content.ReadFromJsonAsync<UrunDetayDto>())!;
        var diyot = (await c.GetFromJsonAsync<ParcaOzetDto>("/api/parcalar/bul?metin=D-1N4007"))!;
        y = await c.PutAsJsonAsync($"/api/urunler/{urun.Id}/agac", new[] { new UrunAgaciSatiriIstek(kasa.Id, 1), new UrunAgaciSatiriIstek(diyot.Id, 2) });
        Assert.Equal(2, (await y.Content.ReadFromJsonAsync<UrunDetayDto>())!.Agac.Count);

        await c.PostAsJsonAsync("/api/stok/giris", new MalGirisiIstek(kasa.Id, "Yeni Kasa A.Ş.", "L-KASA-1", 5, new(2026, 9, 1)));
        y = await c.PostAsJsonAsync("/api/uretim", new UretimIstek(urun.Id, 2));
        Assert.Equal(["SN-KT-0001", "SN-KT-0002"], (await y.Content.ReadFromJsonAsync<UretimSonucuDto>())!.SeriNolar);
        await c.PostAsJsonAsync("/api/satis", new SatisIstek("Gama Ltd", ["sn-kt-0001"]));

        var ara = (await c.GetFromJsonAsync<NumaraAramaDto>("/api/izleme/ara?q=l-kasa-1"))!;
        Assert.Equal(("lot", "L-KASA-1"), (ara.Tur, ara.Deger));
        var lot = (await c.GetFromJsonAsync<LotIzDto>("/api/izleme/lot/L-KASA-1"))!;
        Assert.Equal(["SN-KT-0001", "SN-KT-0002"], lot.Uretimler.Select(u => u.SeriNo));
        Assert.Equal("Gama Ltd", Assert.Single(lot.Musteriler).Ad);
        var seri = (await c.GetFromJsonAsync<SeriIzDto>("/api/izleme/seri/SN-KT-0002"))!;
        Assert.Null(seri.Satis);
        Assert.Equal(("D-1N4007", 2), (seri.Parcalar[0].Parca.Kod, seri.Parcalar[0].Adet));
        Assert.Equal("satis", (await c.GetFromJsonAsync<List<HareketDto>>("/api/izleme/hareketler"))![0].Tur);
        Assert.Contains("Gama Ltd", (await c.GetFromJsonAsync<List<IletisimDto>>("/api/musteriler"))!.Select(m => m.Ad));

        var stok = (await c.GetFromJsonAsync<List<StokGrubuDto>>("/api/stok?parca=KASA-1"))!;
        Assert.Equal((3, true), (stok.Single().Parca.KullanilabilirStok, stok.Single().Lotlar.Single().Sirada));
    }

    // --- Swagger ------------------------------------------------------------------------

    [Fact]
    public async Task Swagger_bearer_yetkilendirmesiyle_acilir()
    {
        Assert.Equal(HttpStatusCode.OK, (await Istemci().GetAsync("/swagger/index.html")).StatusCode);
        var belge = JsonDocument.Parse(await Istemci().GetStringAsync("/swagger/v1/swagger.json")).RootElement;
        var bearer = belge.GetProperty("components").GetProperty("securitySchemes").GetProperty("Bearer");
        Assert.Equal(("http", "bearer"), (bearer.GetProperty("type").GetString(), bearer.GetProperty("scheme").GetString()));
        Assert.True(belge.GetProperty("security")[0].TryGetProperty("Bearer", out _));
        Assert.True(belge.GetProperty("paths").TryGetProperty("/api/kimlik/giris", out _));
    }

    private async Task<int> LotId(string lotNo)
    {
        await using var db = vt.YeniContext();
        return await db.StokLotlari.Where(l => l.LotNo == lotNo).Select(l => l.Id).SingleAsync();
    }
}
