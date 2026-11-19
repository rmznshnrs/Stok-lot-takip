namespace EntityLayer.Concrete;

public class SatisIade : IDenetimli
{
    public int Id { get; set; }
    public int SatisId { get; set; }
    public Satis? Satis { get; set; }
    public int UretimId { get; set; }
    public Uretim? Uretim { get; set; }
    public required string Aciklama { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
}
