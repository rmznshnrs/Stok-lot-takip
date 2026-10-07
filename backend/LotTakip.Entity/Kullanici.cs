namespace LotTakip.Entity;

public enum KullaniciRolu
{
    Kullanici,
    Admin,
}

public class Kullanici
{
    public int Id { get; set; }
    public required string Ad { get; set; }
    public required string Soyad { get; set; }
    public required string KullaniciAdi { get; set; }
    /// <summary>BCrypt özeti; şifrenin kendisi saklanmaz.</summary>
    public required string SifreHash { get; set; }
    public KullaniciRolu Rol { get; set; } = KullaniciRolu.Kullanici;
}
