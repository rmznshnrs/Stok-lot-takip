namespace EntityLayer.Concrete;

public class Uretim : IDenetimli
{
    public int Id { get; set; }
    public int UrunId { get; set; }
    public Urun? Urun { get; set; }
    public required string SeriNo { get; set; }
    public DateTimeOffset Tarih { get; set; }

    public int? UretimEmriId { get; set; }
    public UretimEmri? UretimEmri { get; set; }
    public int? SiparisKalemiId { get; set; }
    public SiparisKalemi? SiparisKalemi { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<UretimTuketim> Tuketimler { get; set; } = [];
    public SatisKalemi? SatisKalemi { get; set; }
    public UretimHurda? Hurda { get; set; }
    public List<SatisIade> Iadeler { get; set; } = [];
    public List<UrunGeriCagirma> GeriCagirmalar { get; set; } = [];
}
