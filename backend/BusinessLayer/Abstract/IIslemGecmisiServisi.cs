namespace BusinessLayer.Abstract;

public interface IIslemGecmisiServisi
{
    /// <summary>
    /// Yalnız Admin: kim, ne zaman, ne yaptı (yeniden eskiye). Tür: giris, uretim, satis, iade, duzeltme, hurda.
    /// Aynı anda yapılan üretim, iade ve hurda tek satırdır. "once" verilirse ondan eski kayıtlar (sayfalama).
    /// </summary>
    Task<List<IslemDto>> IslemlerAsync(int kullaniciId, string? tur = null, int? yapanId = null, DateTimeOffset? once = null,
                                       int adet = 50, CancellationToken ct = default);
}
