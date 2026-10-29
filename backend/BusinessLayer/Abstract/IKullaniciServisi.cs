namespace BusinessLayer.Abstract;

public interface IKullaniciServisi
{
    /// <summary>Kullanıcı adı ve şifreyi doğrular (BCrypt). Hatalıysa null; hangisinin hatalı olduğu söylenmez.</summary>
    Task<KullaniciDto?> DogrulaAsync(string kullaniciAdi, string sifre, CancellationToken ct = default);

    Task<KullaniciDto?> GetirAsync(int id, CancellationToken ct = default);

    /// <summary>Kendi şifresini değiştirir; eski şifre doğru olmalı.</summary>
    Task SifreDegistirAsync(int kullaniciId, SifreDegistirIstek istek, CancellationToken ct = default);

    // Kullanıcı yönetimi: yalnız Admin
    Task<List<KullaniciDto>> ListeleAsync(int islemYapanId, CancellationToken ct = default);
    Task<KullaniciDto> OlusturAsync(KullaniciOlusturIstek istek, int islemYapanId, CancellationToken ct = default);
    /// <summary>Son Admin'in rolü Kullanici yapılamaz (sistem yöneticisiz kalmasın).</summary>
    Task<KullaniciDto> GuncelleAsync(int id, KullaniciGuncelleIstek istek, int islemYapanId, CancellationToken ct = default);
    /// <summary>Hiç işlemi yoksa siler, varsa pasif yapar. Kendini ve son Admin'i silemez.</summary>
    Task<KullaniciSilSonucuDto> SilAsync(int id, int islemYapanId, CancellationToken ct = default);
    Task<KullaniciDto> EtkinlestirAsync(int id, int islemYapanId, CancellationToken ct = default);
}
