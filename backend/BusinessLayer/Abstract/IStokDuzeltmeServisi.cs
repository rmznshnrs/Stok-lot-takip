namespace BusinessLayer.Abstract;

public interface IStokDuzeltmeServisi
{
    Task<StokDuzeltmeSonucuDto> DuzeltAsync(StokDuzeltmeIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<StokDuzeltmeSonucuDto> HataliAsync(StokHataliIstek istek, int kullaniciId, CancellationToken ct = default);
}
