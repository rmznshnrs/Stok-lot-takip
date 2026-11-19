namespace EntityLayer.Concrete;

public class SifreSifirlama
{
    public int Id { get; set; }
    public int KullaniciId { get; set; }
    public Kullanici? Kullanici { get; set; }
    public required string KodOzeti { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
    public DateTimeOffset SonGecerlilik { get; set; }
    public DateTimeOffset? KullanilmaZamani { get; set; }
}
