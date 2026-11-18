namespace BusinessLayer.Abstract;

/// <summary>Excel raporu: son 1 / 3 / 6 / 12 ay; özet, siparişler, üretim sırası, üretim, sevkiyat, mal girişi, parça stoğu, geri çağırmalar.</summary>
public interface IRaporServisi
{
    Task<(byte[] Icerik, string DosyaAdi)> ExcelAsync(int ay, CancellationToken ct = default);
}
