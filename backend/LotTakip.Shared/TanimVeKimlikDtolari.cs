using System.ComponentModel.DataAnnotations;

namespace LotTakip.Shared;

// --- Kimlik ve kullanıcılar -------------------------------------------------------

public static class Roller
{
    public const string Admin = "Admin";
    public const string Kullanici = "Kullanici";
}

public record GirisIstek([Required] string KullaniciAdi, [Required] string Sifre);

public record KullaniciDto(int Id, string Ad, string Soyad, string KullaniciAdi, string Rol);

/// <summary>Token: "Authorization: Bearer {Token}" başlığıyla gönderilir.</summary>
public record GirisSonucuDto(string Token, DateTimeOffset GecerlilikSonu, KullaniciDto Kullanici);

public record SifreDegistirIstek([Required] string EskiSifre, [Required] string YeniSifre);

public record KullaniciOlusturIstek(
    [Required] string Ad, [Required] string Soyad, [Required] string KullaniciAdi,
    [Required] string Sifre, string Rol = Roller.Kullanici);

/// <summary>YeniSifre boşsa şifre değişmez.</summary>
public record KullaniciGuncelleIstek([Required] string Ad, [Required] string Soyad, [Required] string Rol, string? YeniSifre = null);

// --- Tanımlar: parça, ürün, ürün ağacı -------------------------------------------

public record ParcaIstek([Required] string Kod, [Required] string Ad, string Birim = "adet", int MinStok = 0);

/// <summary>SeriOneki boşsa yeni üründe "Kod-" olur, var olan üründe değişmez.</summary>
public record UrunIstek([Required] string Kod, [Required] string Ad, string? SeriOneki = null);

public record UrunAgaciSatiriIstek(int ParcaId, int Adet);

public record UrunListeDto(int Id, string Kod, string Ad, string SeriOneki, int ParcaCesidi, int UretimSayisi);

public record UrunAgaciSatiriDto(int ParcaId, string ParcaKod, string ParcaAd, string Birim, int Adet,
                                 int KullanilabilirStok, bool MinAltinda);

/// <summary>SeriOnekiDegistirilebilir: üretim yapılmışsa önek değişmez (seri no sırası bölünmesin).</summary>
public record UrunDetayDto(int Id, string Kod, string Ad, string SeriOneki, bool SeriOnekiDegistirilebilir,
                           IReadOnlyList<UrunAgaciSatiriDto> Agac);

/// <summary>Iletisim: telefon · e-posta · adres tek satırda (görüntüleme için).</summary>
public record IletisimDto(int Id, string Ad, string Iletisim, string Telefon = "", string Eposta = "", string Adres = "");

/// <summary>Müşteri veya tedarikçi: ad zorunlu ve tekil, iletişim serbest metin.</summary>
/// <summary>Telefon / Eposta / Adres ayrı ayrı. Yalnız Iletisim gelirse (eski istemciler) adres sayılır.</summary>
public record IletisimIstek(string Ad, string? Iletisim = null, string? Telefon = null, string? Eposta = null, string? Adres = null);

// --- İşlem istekleri ---------------------------------------------------------------

public record UretimIstek(int UrunId, int Adet);


public record SatisUyarilariIstek(IReadOnlyList<string> SeriNolar);

// --- Hesap: kayıt ol, şifremi unuttum -------------------------------------------

public record KayitIstek(
    [Required] string Ad, [Required] string Soyad, [Required] string KullaniciAdi, [Required] string Eposta,
    [Required] string Sifre);

public record OnayBekleyenDto(int Id, string Ad, string Soyad, string KullaniciAdi, string? Eposta);

public record SifreUnuttumIstek([Required] string KullaniciAdiVeyaEposta);

public record SifreSifirlaIstek([Required] string Kod, [Required] string YeniSifre);

public record EpostaIstek(string? Eposta);

public record EpostaDto(string? Eposta);
