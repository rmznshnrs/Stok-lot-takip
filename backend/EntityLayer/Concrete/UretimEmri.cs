namespace EntityLayer.Concrete;

public enum UretimEmriDurumu
{
    Bekliyor,
    Tamamlandi,
    Iptal,
}

public class UretimEmri : IDenetimli
{
    public int Id { get; set; }
    public int UrunId { get; set; }
    public Urun? Urun { get; set; }
    public int Adet { get; set; }
    public int Uretilen { get; set; }
    public int Sira { get; set; }
    public DateOnly PlanlananTarih { get; set; }
    public UretimEmriDurumu Durum { get; set; }
    public string Aciklama { get; set; } = "";

    public int? SiparisKalemiId { get; set; }
    public SiparisKalemi? SiparisKalemi { get; set; }

    public DateTimeOffset? BaslamaZamani { get; set; }
    public DateTimeOffset? BitisZamani { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<UretimEmriParcasi> Parcalar { get; set; } = [];
    public List<Uretim> Uretimler { get; set; } = [];
}

public class UretimEmriParcasi
{
    public int UretimEmriId { get; set; }
    public UretimEmri? UretimEmri { get; set; }
    public int ParcaId { get; set; }
    public Parca? Parca { get; set; }
    public int BirimAdet { get; set; }
}
