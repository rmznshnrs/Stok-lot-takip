using System.ComponentModel.DataAnnotations;

namespace SharedLayer.DTOs;

public static class Roller
{
    public const string Admin = "Admin";
    public const string Kullanici = "Kullanici";
}

public record GirisIstek([Required] string KullaniciAdi, [Required] string Sifre);

public record KullaniciDto(int Id, string Ad, string Soyad, string KullaniciAdi, string Rol, bool Aktif = true);

public record KullaniciSilSonucuDto(bool Silindi);

public record GirisSonucuDto(string Token, DateTimeOffset GecerlilikSonu, KullaniciDto Kullanici);

public record SifreDegistirIstek([Required] string EskiSifre, [Required] string YeniSifre);

public record KullaniciOlusturIstek(
    [Required] string Ad, [Required] string Soyad, [Required] string KullaniciAdi,
    [Required] string Sifre, string Rol = Roller.Kullanici);

public record KullaniciGuncelleIstek([Required] string Ad, [Required] string Soyad, [Required] string Rol, string? YeniSifre = null);

public record ParcaIstek([Required] string Kod, [Required] string Ad, int MinStok = 0, string? Aciklama = null);

public record UrunIstek([Required] string Kod, [Required] string Ad, string? SeriOneki = null, string? Aciklama = null);

public record UrunAgaciSatiriIstek(int ParcaId, int Adet);

public record UrunListeDto(int Id, string Kod, string Ad, string SeriOneki, int ParcaCesidi, int UretimSayisi, string Aciklama = "");

public record UrunAgaciSatiriDto(int ParcaId, string ParcaKod, string ParcaAd, int Adet,
                                 int KullanilabilirStok, bool MinAltinda);

public record UrunDetayDto(int Id, string Kod, string Ad, string SeriOneki, bool SeriOnekiDegistirilebilir,
                           IReadOnlyList<UrunAgaciSatiriDto> Agac, string Aciklama = "");

public record IletisimDto(int Id, string Ad, string Iletisim, string Telefon = "", string Eposta = "", string Adres = "");

public record IletisimIstek(string Ad, string? Iletisim = null, string? Telefon = null, string? Eposta = null, string? Adres = null);

public record SatisUyarilariIstek(IReadOnlyList<string> SeriNolar);

public record KayitIstek(
    [Required] string Ad, [Required] string Soyad, [Required] string KullaniciAdi, [Required] string Eposta,
    [Required] string Sifre);

public record OnayBekleyenDto(int Id, string Ad, string Soyad, string KullaniciAdi, string? Eposta);

public record SifreUnuttumIstek([Required] string KullaniciAdiVeyaEposta);

public record SifreSifirlaIstek([Required] string Kod, [Required] string YeniSifre);

public record EpostaIstek(string? Eposta);

public record EpostaDto(string? Eposta);
