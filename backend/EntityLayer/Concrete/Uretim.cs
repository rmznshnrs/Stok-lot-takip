namespace EntityLayer.Concrete;

/// <summary>
/// Üretilen tek bir ürün; her kaydın tek seri numarası vardır. Aynı anda üretilenler
/// ortak bir ürün lot numarası taşır (seri öneki + L + sıra: U1-L001).
/// </summary>
public class Uretim : IDenetimli
{
    public int Id { get; set; }
    public int UrunId { get; set; }
    public Urun? Urun { get; set; }
    public required string SeriNo { get; set; }
    public string LotNo { get; set; } = "";
    public DateTimeOffset Tarih { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<UretimTuketim> Tuketimler { get; set; } = [];
    public SatisKalemi? SatisKalemi { get; set; }
    public UretimHurda? Hurda { get; set; }
    public List<SatisIade> Iadeler { get; set; } = [];
    public List<UrunGeriCagirma> GeriCagirmalar { get; set; } = [];
}
