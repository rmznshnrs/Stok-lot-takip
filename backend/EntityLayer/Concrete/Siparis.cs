namespace EntityLayer.Concrete;

public class Siparis : IDenetimli
{
    public int Id { get; set; }
    public required string No { get; set; }
    public int MusteriId { get; set; }
    public Musteri? Musteri { get; set; }
    public DateOnly Tarih { get; set; }
    public DateOnly? IstenenTarih { get; set; }
    public string Aciklama { get; set; } = "";

    public DateTimeOffset? IptalZamani { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<SiparisKalemi> Kalemler { get; set; } = [];
    public List<Satis> Sevkiyatlar { get; set; } = [];
}

public class SiparisKalemi
{
    public int Id { get; set; }
    public int SiparisId { get; set; }
    public Siparis? Siparis { get; set; }
    public int UrunId { get; set; }
    public Urun? Urun { get; set; }
    public int Adet { get; set; }

    public List<Uretim> Ayrilanlar { get; set; } = [];
    public List<UretimEmri> UretimEmirleri { get; set; } = [];
}
