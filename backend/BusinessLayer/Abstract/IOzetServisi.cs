namespace BusinessLayer.Abstract;

/// <summary>Ana sayfa özeti ve üretim / sipariş takvimi.</summary>
public interface IOzetServisi
{
    /// <summary>
    /// Genel durum sayıları, serbest parça stoğuyla en fazla 3 adet
    /// üretilebilen ürünler ve 7 gün içinde (ya da gecikmiş) teslimi istenen açık siparişler.
    /// </summary>
    Task<AnaSayfaOzetiDto> AnaSayfaAsync(CancellationToken ct = default);

    /// <summary>Gün gün, ürün ürün planlanan üretim, üretilen ve teslimi istenen sipariş adedi (en çok 62 gün).</summary>
    Task<List<TakvimGunuDto>> TakvimAsync(DateOnly baslangic, DateOnly bitis, CancellationToken ct = default);
}
