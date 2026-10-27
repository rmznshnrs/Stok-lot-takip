namespace BusinessLayer.Abstract;

public interface IStokServisi
{
    /// <summary>Parçalar ve stok bilgileri (toplam, kullanılabilir, lot sayısı, kullanıldığı ürünler).</summary>
    Task<List<ParcaStokDto>> ParcaStoklariAsync(string? arama = null, CancellationToken ct = default);

    /// <summary>
    /// Parça bazında lotlar (FIFO sırasıyla) ve sıradaki lot. Arama parça kodu/adı veya lot no'da;
    /// parça eşleşirse tüm lotları, yalnız lot eşleşirse eşleşen lotları gösterir.
    /// bitenler=false ise kalanı 0 olan lotlar gizlenir.
    /// </summary>
    Task<List<StokGrubuDto>> StokDurumuAsync(string? arama = null, string? parcaKod = null, bool bitenler = false,
                                             CancellationToken ct = default);

    /// <summary>Yeni lot; tedarikçi adı yoksa tedarikçiyi de oluşturur.</summary>
    Task<MalGirisiSonucuDto> MalGirisiAsync(MalGirisiIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Kod, ad veya "KOD – Ad" yazımından parçayı bulur; kısmi tek eşleşmeyi kabul eder.</summary>
    Task<ParcaOzetDto> ParcaBulAsync(string metin, CancellationToken ct = default);
}
