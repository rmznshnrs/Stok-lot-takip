namespace EntityLayer.Concrete;

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
    public bool Tamamlandi { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }
}
