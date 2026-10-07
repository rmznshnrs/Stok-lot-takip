namespace LotTakip.Entity;

/// <summary>Bir üretimde hangi lottan kaç adet parça kullanıldı (izlenebilirliğin kalbi).</summary>
public class UretimTuketim
{
    public int UretimId { get; set; }
    public Uretim? Uretim { get; set; }
    public int StokLotuId { get; set; }
    public StokLotu? StokLotu { get; set; }
    public int Adet { get; set; }
}
