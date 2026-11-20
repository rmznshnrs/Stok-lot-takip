namespace EntityLayer.Concrete;

public class Parca
{
    public int Id { get; set; }
    public required string Kod { get; set; }
    public required string Ad { get; set; }
    public int MinStok { get; set; }
    public string Aciklama { get; set; } = "";

    public List<StokLotu> Lotlar { get; set; } = [];
    public List<UrunAgaci> AgacSatirlari { get; set; } = [];
}
