namespace BusinessLayer.Abstract;

public interface IUrunLotServisi
{
    /// <summary>Ürün lotları (yeniden eskiye): adetler ve hata işareti. urunId verilirse yalnız o ürünün.</summary>
    Task<List<UrunLotDto>> ListeAsync(int? urunId = null, CancellationToken ct = default);

    /// <summary>
    /// Yalnız Admin: ürün lotunu hatalı işaretler / hatayı kaldırır. Ürünler kendiliğinden geri çağrılmaz;
    /// yalnız GeriCagrilacaklar'da seçilen (bu lottaki) seri numaraları geri çağrılır.
    /// </summary>
    Task<UrunLotSorunSonucuDto> SorunAsync(string lotNo, UrunLotSorunIstek istek, int kullaniciId, CancellationToken ct = default);
}
