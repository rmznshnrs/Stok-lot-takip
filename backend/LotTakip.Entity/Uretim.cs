namespace LotTakip.Entity;

/// <summary>Üretilen tek bir ürün; her kaydın tek seri numarası vardır.</summary>
public class Uretim : IDenetimli
{
    public int Id { get; set; }
    public int UrunId { get; set; }
    public Urun? Urun { get; set; }
    public required string SeriNo { get; set; }
    public DateTimeOffset Tarih { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<UretimTuketim> Tuketimler { get; set; } = [];
    public SatisKalemi? SatisKalemi { get; set; }
}
