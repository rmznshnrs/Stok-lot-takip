namespace LotTakip.Entity;

public class Musteri
{
    public int Id { get; set; }
    public required string Ad { get; set; }
    public string Iletisim { get; set; } = "";

    public List<Satis> Satislar { get; set; } = [];
}
