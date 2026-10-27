namespace BusinessLayer.Abstract;

public interface IGeriCagirmaServisi
{
    /// <summary>
    /// Yalnız Admin: seri numaralı ürünleri geri çağırır (açıklama zorunlu; sebep lot verilebilir).
    /// Bulunamayan, hurdaya ayrılmış ya da zaten açık geri çağırması olan varsa hiçbiri çağrılmaz.
    /// </summary>
    Task<List<GeriCagirmaDto>> GeriCagirAsync(GeriCagirmaIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Yalnız Admin: müşterideki ürün geri alındı. Satış iadesi yapılır, ürün depoya döner (satışa kapalı kalır).</summary>
    Task<GeriCagirmaDto> GeriAlAsync(int id, int kullaniciId, CancellationToken ct = default);

    /// <summary>Yalnız Admin: depodaki geri çağrılmış ürün yeniden satışa açılır; geri çağırma kapanır.</summary>
    Task<GeriCagirmaDto> SatisaAcAsync(int id, int kullaniciId, CancellationToken ct = default);

    /// <summary>
    /// Yalnız Admin: sorun yok, ürün müşteriye geri verilir; geri çağırma kapanır. Müşterideyse orada kalır;
    /// bu geri çağırmayla geri alınmışsa aynı müşteriye bugünkü tarihle yeni satış yapılır.
    /// </summary>
    Task<GeriCagirmaDto> MusteriyeVerAsync(int id, int kullaniciId, CancellationToken ct = default);

    /// <summary>Geri çağırmalar (yeniden eskiye). acik=true: yalnız sonuçlanmamışlar (müşteride, depoda, geri alındı).</summary>
    Task<List<GeriCagirmaDto>> ListeAsync(bool acik = false, string? arama = null, CancellationToken ct = default);
}
