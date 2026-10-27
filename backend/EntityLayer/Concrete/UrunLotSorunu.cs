namespace EntityLayer.Concrete;

/// <summary>
/// Hatalı işaretlenen ürün lotu (aynı anda üretilenler). Lottaki ürünler kendiliğinden geri çağrılmaz;
/// HataliAdet kullanıcının belirttiği hatalı ürün sayısıdır (0: belirtilmedi). Hata kaldırılınca kayıt silinir.
/// </summary>
public class UrunLotSorunu : IDenetimli
{
    public int Id { get; set; }
    public int UrunId { get; set; }
    public Urun? Urun { get; set; }
    public required string LotNo { get; set; }
    public required string Aciklama { get; set; }
    public int HataliAdet { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
}
