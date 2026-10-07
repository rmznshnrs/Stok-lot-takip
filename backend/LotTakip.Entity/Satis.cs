namespace LotTakip.Entity;

public class Satis : IDenetimli
{
    public int Id { get; set; }
    public int MusteriId { get; set; }
    public Musteri? Musteri { get; set; }
    public DateOnly Tarih { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<SatisKalemi> Kalemler { get; set; } = [];
}
