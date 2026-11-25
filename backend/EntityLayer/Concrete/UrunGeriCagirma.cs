namespace EntityLayer.Concrete;

public class UrunGeriCagirma : IDenetimli
{
    public int Id { get; set; }
    public int UretimId { get; set; }
    public Uretim? Uretim { get; set; }
    public int? StokLotuId { get; set; }
    public StokLotu? StokLotu { get; set; }
    public required string Aciklama { get; set; }

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public DateTimeOffset? BilgiVerildiZamani { get; set; }
    public DateTimeOffset? GeriAlinmaZamani { get; set; }
    public DateTimeOffset? KapanmaZamani { get; set; }
    public bool MusteriyeVerildi { get; set; }
    public string GeriAlmaKargoFirmasi { get; set; } = "";
    public string GeriAlmaTakipNo { get; set; } = "";
    public string GeriGondermeKargoFirmasi { get; set; } = "";
    public string GeriGondermeTakipNo { get; set; } = "";
    public int? KapatanKullaniciId { get; set; }
    public Kullanici? KapatanKullanici { get; set; }
}
