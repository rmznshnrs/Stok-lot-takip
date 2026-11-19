namespace EntityLayer.Concrete;

public class StokLotu : IDenetimli
{
    public int Id { get; set; }
    public int ParcaId { get; set; }
    public Parca? Parca { get; set; }
    public int TedarikciId { get; set; }
    public Tedarikci? Tedarikci { get; set; }

    public required string LotNo { get; set; }
    public string TedarikciLotNo { get; set; } = "";
    public DateOnly SiparisTarihi { get; set; }
    public string SiparisNo { get; set; } = "";
    public int GirisAdet { get; set; }
    public int KalanAdet { get; set; }
    public bool GeriCagrildi { get; set; }

    public bool Sorunlu { get; set; }
    public string SorunAciklama { get; set; } = "";
    public DateTimeOffset? SorunZamani { get; set; }
    public int? SorunIsaretleyenId { get; set; }
    public Kullanici? SorunIsaretleyen { get; set; }
    public DateTimeOffset GirisZamani { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<UretimTuketim> Tuketimler { get; set; } = [];
    public List<StokDuzeltme> Duzeltmeler { get; set; } = [];
}
