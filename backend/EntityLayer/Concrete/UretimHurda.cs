namespace EntityLayer.Concrete;

public enum UretimHurdaTuru
{
    Hurda,
    Kayip,
}

public class UretimHurda : IDenetimli
{
    public int Id { get; set; }
    public int UretimId { get; set; }
    public Uretim? Uretim { get; set; }
    public UretimHurdaTuru Tur { get; set; }
    public required string Aciklama { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
}
