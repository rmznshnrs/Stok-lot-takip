namespace LotTakip.Entity;

public class Urun
{
    public int Id { get; set; }
    public required string Kod { get; set; }
    public required string Ad { get; set; }
    /// <summary>Seri numarası öneki, ör. "SN-IP-" → SN-IP-0001.</summary>
    public required string SeriOneki { get; set; }

    public List<UrunAgaci> Agac { get; set; } = [];
    public List<Uretim> Uretimler { get; set; } = [];
}
