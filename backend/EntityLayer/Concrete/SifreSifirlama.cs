namespace EntityLayer.Concrete;

/// <summary>
/// "Şifremi unuttum" isteği: kullanıcının e-postasına tek kullanımlık bir kod gider. Kodun kendisi değil
/// SHA-256 özeti saklanır; süresi dolan ya da kullanılan kod bir daha işe yaramaz.
/// </summary>
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
