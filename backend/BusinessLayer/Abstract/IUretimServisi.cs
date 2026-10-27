namespace BusinessLayer.Abstract;

public interface IUretimServisi
{
    /// <summary>
    /// Kaydetmeden: her parça için hangi lottan kaç adet düşüleceği (FIFO), eksikler, atlanacak
    /// geri çağrılmış lotlar ve verilecek seri numaraları.
    /// </summary>
    Task<UretimOnizlemeDto> OnizleAsync(int urunId, int adet, CancellationToken ct = default);

    /// <summary>
    /// N adet üretir: N Uretim kaydı, ardışık seri no, FIFO lot düşümü, tek transaction.
    /// Geri çağrılan lotlar kullanılmaz. Stok yetmezse hiçbir şey kaydedilmez (YetersizStokHatasi).
    /// Kullanılan lotlar transaction boyunca kilitlenir; aynı anda yapılan üretimler sırayla işler.
    /// </summary>
    Task<UretimSonucuDto> UretAsync(int urunId, int adet, int kullaniciId, DateTimeOffset? tarih = null,
                                    CancellationToken ct = default);

    /// <summary>Şimdi üretilirse bu partiye verilecek ürün lot numarası (seri öneki + L + 3 hane: U1-L001).</summary>
    Task<string> SonrakiLotNoAsync(int urunId, CancellationToken ct = default);

    /// <summary>Şimdi üretilirse verilecek ardışık seri numaraları.</summary>
    Task<List<string>> SonrakiSeriNolarAsync(int urunId, int adet, CancellationToken ct = default);

    /// <summary>
    /// Üretim geçmişi: her seri no bir satır (yeniden eskiye), satış durumu ve geri çağrılan lotlarıyla.
    /// Arama seri no, ürün adı veya müşteri adında.
    /// </summary>
    Task<List<UretimGecmisiDto>> UretimGecmisiAsync(string? arama = null, int? urunId = null, int adet = 500,
                                                    CancellationToken ct = default);

    /// <summary>
    /// Admin: depodaki seri numaralarını hurdaya / kayba ayırır (açıklama zorunlu). Bulunamayan, satılmış
    /// veya zaten ayrılmış seri no varsa hiçbiri ayrılmaz. Ayrılan ürün satılamaz.
    /// </summary>
    Task<UretimHurdaSonucuDto> HurdayaAyirAsync(UretimHurdaIstek istek, int kullaniciId, CancellationToken ct = default);
}
