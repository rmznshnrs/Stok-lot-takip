namespace LotTakip.Entity;

public class Tedarikci
{
    public int Id { get; set; }
    public required string Ad { get; set; }
    public string Iletisim { get; set; } = "";

    public List<StokLotu> Lotlar { get; set; } = [];
}
