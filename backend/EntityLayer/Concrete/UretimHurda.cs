namespace EntityLayer.Concrete;

public enum UretimHurdaTuru
{
    Hurda,
    Kayip,
}

/// <summary>Depodaki bir seri no'nun hurdaya / kayba ayrılması (Admin, açıklamalı). Ayrılan ürün satılamaz.</summary>
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
