namespace EntityLayer.Concrete;

public enum SevkiyatDurumu
{
    Hazirlaniyor,
    Kargoda,
    TeslimEdildi,
}

public class Satis : IDenetimli
{
    public int Id { get; set; }
    public string? No { get; set; }
    public int MusteriId { get; set; }
    public Musteri? Musteri { get; set; }
    public int? SiparisId { get; set; }
    public Siparis? Siparis { get; set; }
    public DateOnly Tarih { get; set; }
    public DateTimeOffset? Zaman { get; set; }

    public SevkiyatDurumu Durum { get; set; }
    public string KargoFirmasi { get; set; } = "";
    public string TakipNo { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public DateTimeOffset? KargoZamani { get; set; }
    public DateTimeOffset? TeslimZamani { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<SatisKalemi> Kalemler { get; set; } = [];
    public List<SatisIade> Iadeler { get; set; } = [];
}
