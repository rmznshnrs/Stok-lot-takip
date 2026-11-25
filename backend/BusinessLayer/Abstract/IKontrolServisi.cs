namespace BusinessLayer.Abstract;

public interface IKontrolServisi
{
    Task<NumaraKontrolDto> NumaraAsync(string tur, string deger, int? haricId = null, CancellationToken ct = default);
}
