namespace LotTakip.Entity;

/// <summary>
/// İşlemi yapan kullanıcıyı ve zamanı tutan kayıtlar (StokLotu, Uretim, Satis, StokDuzeltme).
/// </summary>
public interface IDenetimli
{
    int OlusturanKullaniciId { get; set; }
    Kullanici? OlusturanKullanici { get; set; }
    DateTimeOffset OlusturmaZamani { get; set; }
}
