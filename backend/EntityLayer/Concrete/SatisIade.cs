namespace EntityLayer.Concrete;

/// <summary>
/// Satılmış bir seri no'nun iadesi. İade alınınca satış kalemi kaldırılır (ürün depoya döner ve yeniden
/// satılabilir), satış kaydı ve hangi müşteriye satıldığı bu kayıtta korunur.
/// </summary>
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
