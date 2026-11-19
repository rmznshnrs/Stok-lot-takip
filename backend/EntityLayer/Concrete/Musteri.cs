namespace EntityLayer.Concrete;

public class Musteri
{
    public int Id { get; set; }
    public required string Ad { get; set; }
    public string Telefon { get; set; } = "";
    public string Eposta { get; set; } = "";
    public string Adres { get; set; } = "";
    public string Iletisim { get; private set; } = "";

    public List<Satis> Satislar { get; set; } = [];
    public List<Siparis> Siparisler { get; set; } = [];
}
