namespace BusinessLayer.Abstract;

public interface IIslemGecmisiServisi
{
    Task<List<IslemDto>> IslemlerAsync(int kullaniciId, string? tur = null, int? yapanId = null, DateTimeOffset? once = null,
                                       int adet = 50, CancellationToken ct = default);
}
