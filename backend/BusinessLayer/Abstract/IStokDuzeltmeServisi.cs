namespace BusinessLayer.Abstract;

public interface IStokDuzeltmeServisi
{
    /// <summary>
    /// Admin'in açıklamalı stok düzeltmesi (sayım farkı, fire, diğer). Miktar + veya -;
    /// kalan adet negatife düşemez.
    /// </summary>
    Task<StokDuzeltmeSonucuDto> DuzeltAsync(StokDuzeltmeIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>
    /// Admin: bir stok girişinden hatalı çıkan parça adedi (açıklama zorunlu). StoktanDus ise kalan adetten düşülür,
    /// değilse yalnız sayı kaydedilir. Parça listesindeki hatalı adet bu kayıtların toplamıdır.
    /// </summary>
    Task<StokDuzeltmeSonucuDto> HataliAsync(StokHataliIstek istek, int kullaniciId, CancellationToken ct = default);
}
