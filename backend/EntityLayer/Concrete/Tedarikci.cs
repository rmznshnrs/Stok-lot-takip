namespace EntityLayer.Concrete;

public class Tedarikci
{
    public int Id { get; set; }
    public required string Ad { get; set; }
    public string Telefon { get; set; } = "";
    public string Eposta { get; set; } = "";
    public string Adres { get; set; } = "";
    /// <summary>Veritabanında hesaplanır: telefon · e-posta · adres (boş olanlar atlanır). Yalnız okunur.</summary>
    public string Iletisim { get; private set; } = "";

    public List<StokLotu> Lotlar { get; set; } = [];
}
