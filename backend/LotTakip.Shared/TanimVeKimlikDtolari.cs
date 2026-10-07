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

public record UrunIstek([Required] string Kod, [Required] string Ad, [Required] string SeriOneki);

public record UrunAgaciSatiriIstek(int ParcaId, int Adet);

public record UrunListeDto(int Id, string Kod, string Ad, string SeriOneki, int ParcaCesidi, int UretimSayisi);

public record UrunAgaciSatiriDto(int ParcaId, string ParcaKod, string ParcaAd, string Birim, int Adet,
                                 int KullanilabilirStok, bool MinAltinda);

/// <summary>SeriOnekiDegistirilebilir: üretim yapılmışsa önek değişmez (seri no sırası bölünmesin).</summary>
public record UrunDetayDto(int Id, string Kod, string Ad, string SeriOneki, bool SeriOnekiDegistirilebilir,
                           IReadOnlyList<UrunAgaciSatiriDto> Agac);

public record IletisimDto(int Id, string Ad, string Iletisim);

// --- İşlem istekleri ---------------------------------------------------------------

public record UretimIstek(int UrunId, int Adet);

public record GeriCagirIstek(bool GeriCagir = true);

public record SatisUyarilariIstek(IReadOnlyList<string> SeriNolar);
