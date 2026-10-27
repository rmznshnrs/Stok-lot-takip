namespace BusinessLayer.Abstract;

public interface IIzlemeServisi
{
    /// <summary>Parçadan bütüne: lotun kullanıldığı seri numaraları ve satıldığı müşteriler. Yoksa null.</summary>
    Task<LotIzDto?> LotIzleAsync(string lotNo, CancellationToken ct = default);

    /// <summary>Bütünden parçaya: seri no'lu üründeki parçalar, lotları, adetleri ve satış bilgisi. Yoksa null.</summary>
    Task<SeriIzDto?> SeriIzleAsync(string seriNo, CancellationToken ct = default);

    /// <summary>
    /// Girilen numaranın türünü algılar: birebir lot no, seri no veya parça kodu; yoksa numarayı
    /// içeren lot ve seri numaraları aday olarak döner (tek aday varsa doğrudan o seçilir).
    /// </summary>
    Task<NumaraAramaDto> NumaraAraAsync(string metin, CancellationToken ct = default);

    /// <summary>Son mal girişi, üretim ve satışlar (yeniden eskiye). Aynı anda yapılan üretimler tek satır.</summary>
    Task<List<HareketDto>> SonHareketlerAsync(int adet = 8, CancellationToken ct = default);

    /// <summary>
    /// Yalnız Admin: lotu hatalı (sorunlu) işaretler (açıklama zorunlu) ve kalan parçaların üretimde kullanılıp
    /// kullanılmayacağını belirler; Sorunlu=false hatayı kaldırır. Ürünler kendiliğinden geri çağrılmaz;
    /// yalnız GeriCagrilacaklar'da seçilen (bu lottan parça taşıyan) seri numaraları aynı işlemde geri çağrılır.
    /// </summary>
    Task<LotSorunSonucuDto> LotSorunAsync(string lotNo, LotSorunIstek istek, int kullaniciId, CancellationToken ct = default);
}
