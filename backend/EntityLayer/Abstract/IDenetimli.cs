namespace EntityLayer.Abstract;

public interface IDenetimli
{
    int OlusturanKullaniciId { get; set; }
    Kullanici? OlusturanKullanici { get; set; }
    DateTimeOffset OlusturmaZamani { get; set; }
}
