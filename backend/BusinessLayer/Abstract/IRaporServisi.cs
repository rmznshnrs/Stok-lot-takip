namespace BusinessLayer.Abstract;

public interface IRaporServisi
{
    Task<(byte[] Icerik, string DosyaAdi)> ExcelAsync(int ay, CancellationToken ct = default);
}
