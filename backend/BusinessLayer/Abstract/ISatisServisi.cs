namespace BusinessLayer.Abstract;

public interface ISatisServisi
{
    /// <summary>Satış listesine eklenecek seri no'yu doğrular: var mı, daha önce satıldı mı. Değilse SatisHatasi.</summary>
    Task<SatilabilirDto> SatilabilirAsync(string seriNo, CancellationToken ct = default);

    /// <summary>Sorunlu lot içeren ya da hatalı ürün lotundaki seri numaraları ve ilgili lotlar (kaydetmeden kontrol).</summary>
    Task<List<SatisUyarisiDto>> SatisUyarilariAsync(IReadOnlyCollection<string> seriNolar, CancellationToken ct = default);

    /// <summary>
    /// Seri numaralarını müşteriye satar (müşteri yoksa oluşturur). Bulunamayan, tekrarlanan veya
    /// daha önce satılmış seri no varsa hiçbir şey kaydedilmez (SatisHatasi). Sorunlu lot
    /// içeren ürün varsa ve onay verilmediyse GeriCagrilanLotOnayiGerekliHatasi.
    /// </summary>
    Task<SatisSonucuDto> SatAsync(SatisIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Satış geçmişi: her satış + ürün için bir satır (yeniden eskiye). Arama müşteri adı veya seri no'da.</summary>
    Task<List<SatisGecmisiDto>> SatisGecmisiAsync(string? arama = null, int adet = 500, CancellationToken ct = default);

    /// <summary>
    /// Yalnız Admin: satıştan seri numaralarını iade alır (açıklama zorunlu). Satış kalemi kaldırılır, ürün depoya
    /// döner ve yeniden satılabilir; kime satıldığı iade kaydında kalır. Bu satışta olmayan seri no varsa hiçbiri alınmaz.
    /// </summary>
    Task<SatisIadeSonucuDto> IadeAlAsync(int satisId, SatisIadeIstek istek, int kullaniciId, CancellationToken ct = default);
}
