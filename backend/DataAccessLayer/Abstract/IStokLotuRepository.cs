namespace DataAccessLayer.Abstract;

// İnce repository'ler: yalnızca LINQ ile ifade edilemeyen veya birden çok yerde kullanılan
// sorgular. Basit sorgular servislerde doğrudan AppDbContext ile yazılır.

public interface IStokLotuRepository
{
    /// <summary>
    /// Parçaların kullanılabilir lotlarını (geri çağrılmamış, kalanı olan) FIFO sırasıyla getirir
    /// ve transaction sonuna kadar kilitler (UPDLOCK). Aynı anda başka bir üretim aynı lotları
    /// okumak isterse bu transaction bitene kadar bekler. Açık bir transaction içinde çağrılmalı.
    /// </summary>
    Task<List<StokLotu>> KullanilabilirLotlariKilitleAsync(IReadOnlyCollection<int> parcaIdleri, CancellationToken ct = default);
}
