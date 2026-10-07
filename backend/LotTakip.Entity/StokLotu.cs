namespace LotTakip.Entity;

/// <summary>Bir parçanın tek bir mal girişi. FIFO sırası SiparisTarihi'ne göredir.</summary>
public class StokLotu : IDenetimli
{
    public int Id { get; set; }
    public int ParcaId { get; set; }
    public Parca? Parca { get; set; }
    public int TedarikciId { get; set; }
    public Tedarikci? Tedarikci { get; set; }

    public required string LotNo { get; set; }
    public DateOnly SiparisTarihi { get; set; }
    public string SiparisNo { get; set; } = "";
    public int GirisAdet { get; set; }
    public int KalanAdet { get; set; }
    public bool GeriCagrildi { get; set; }
    /// <summary>Malın sisteme girildiği an (son hareketlerde kullanılır; FIFO'yu etkilemez).</summary>
    public DateTimeOffset GirisZamani { get; set; }

    /// <summary>Eşzamanlılık belirteci: aynı lot iki işlemde aynı anda değiştirilemez.</summary>
    public byte[] RowVersion { get; set; } = [];

    public int OlusturanKullaniciId { get; set; }
    public Kullanici? OlusturanKullanici { get; set; }
    public DateTimeOffset OlusturmaZamani { get; set; }

    public List<UretimTuketim> Tuketimler { get; set; } = [];
    public List<StokDuzeltme> Duzeltmeler { get; set; } = [];
}
