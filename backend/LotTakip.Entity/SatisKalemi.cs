namespace LotTakip.Entity;

/// <summary>Satılan bir seri numarası. Bir seri no yalnızca bir kez satılır.</summary>
public class SatisKalemi
{
    public int SatisId { get; set; }
    public Satis? Satis { get; set; }
    public int UretimId { get; set; }
    public Uretim? Uretim { get; set; }
}
