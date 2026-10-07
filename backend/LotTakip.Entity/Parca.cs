namespace LotTakip.Entity;

public class Parca
{
    public int Id { get; set; }
    public required string Kod { get; set; }
    public required string Ad { get; set; }
    public string Birim { get; set; } = "adet";
    public int MinStok { get; set; }

    public List<StokLotu> Lotlar { get; set; } = [];
    public List<UrunAgaci> AgacSatirlari { get; set; } = [];
}
