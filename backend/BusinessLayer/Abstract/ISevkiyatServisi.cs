namespace BusinessLayer.Abstract;

/// <summary>
/// Sevkiyat: siparişe ayrılmış ürünlerin müşteriye gönderilmesi (sevk edilen ürün satılmış sayılır).
/// Durum: Hazırlanıyor → Kargoda (kargo firması ve takip no elle girilir) → Teslim edildi.
/// </summary>
public interface ISevkiyatServisi
{
    /// <summary>Yeniden eskiye. Arama: sevkiyat no, müşteri, sipariş no, takip no, seri no. durum: Hazirlaniyor / Kargoda / TeslimEdildi.</summary>
    /// <summary>baslangic verilirse o tarihten sonraki sevkiyatların hepsi (sınırsız; rapor için).</summary>
    Task<List<SevkiyatDto>> ListeAsync(string? arama = null, string? durum = null, int? musteriId = null, CancellationToken ct = default,
                                       DateOnly? baslangic = null);

    Task<SevkiyatDto> GetirAsync(int id, CancellationToken ct = default);

    /// <summary>Seri numaraları siparişe ayrılmış ve henüz gönderilmemiş olmalı. Hatalı lotlu ürün varsa onay gerekir.</summary>
    Task<SevkiyatDto> OlusturAsync(SevkiyatIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Kargo firması ve takip no kaydedilir; hazırlanıyorsa "Kargoda" olur.</summary>
    Task<SevkiyatDto> KargoAsync(int id, KargoIstek istek, CancellationToken ct = default);

    /// <summary>Teslim edildi (kargodan ya da elden).</summary>
    Task<SevkiyatDto> TeslimAsync(int id, CancellationToken ct = default);
}
