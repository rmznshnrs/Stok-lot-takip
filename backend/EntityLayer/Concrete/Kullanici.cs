namespace EntityLayer.Concrete;

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
    public required string SifreHash { get; set; }
    public KullaniciRolu Rol { get; set; } = KullaniciRolu.Kullanici;
    public string? Eposta { get; set; }
    public bool Onayli { get; set; } = true;
    public bool Aktif { get; set; } = true;
}
