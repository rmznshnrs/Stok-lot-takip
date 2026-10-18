namespace LotTakip.Entity;

/// <summary>
/// Bir seri numaralı ürünün geri çağrılması (ör. içindeki parçanın lotu sorunlu çıktı). Açıkken ürün satılamaz.
/// Müşterideki ürün geri alınınca (iade) depoya döner; depodaki ürün hurdaya ayrılır ya da yeniden satışa açılır.
/// Bir seri no'nun aynı anda tek açık geri çağırması olur.
/// </summary>
public class UrunGeriCagirma : IDenetimli
{
    public int Id { get; set; }
    public int UretimId { get; set; }
    public Uretim? Uretim { get; set; }
    /// <summary>Geri çağırmaya sebep olan sorunlu lot (varsa).</summary>
    public int? StokLotuId { get; set; }
    public StokLotu? StokLotu { get; set; }
    public required string Aciklama { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    /// <summary>Müşteriden geri alındığı an (iade kaydı da oluşur).</summary>
    public DateTimeOffset? GeriAlinmaZamani { get; set; }
    /// <summary>Yeniden satışa açıldığı an; doluysa geri çağırma kapanmıştır.</summary>
    public DateTimeOffset? KapanmaZamani { get; set; }
    public int? KapatanKullaniciId { get; set; }
    public Kullanici? KapatanKullanici { get; set; }
}
