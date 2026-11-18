namespace EntityLayer.Concrete;

/// <summary>
/// Takvimde bir güne girilen hatırlatıcı: parça alımı (parça, adet, tedarikçi isteğe bağlı) ya da serbest not.
/// Stoğa dokunmaz. Ürün üretimi planları ise üretim sırasına iş olarak girer (UretimEmri.PlanlananTarih).
/// </summary>
public class TakvimPlani : IDenetimli
{
    public int Id { get; set; }
    public DateOnly Tarih { get; set; }
    public int? ParcaId { get; set; }
    public Parca? Parca { get; set; }
    public int? Adet { get; set; }
    public int? TedarikciId { get; set; }
    public Tedarikci? Tedarikci { get; set; }
    public string Aciklama { get; set; } = "";
    /// <summary>Hatırlatıcı yapıldı (ör. parça sipariş edildi) olarak işaretlendi.</summary>
    public bool Tamamlandi { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
}
