namespace BusinessLayer.Abstract;

/// <summary>
/// Üretim sırası: ürünler doğrudan üretilmez, önce sıraya girer. Sıraya girince parçalar ayrılır (başka iş
/// kullanamaz), üretildikçe stoktan düşülür ve ürünler stoğa girer.
/// </summary>
public interface IUretimSirasiServisi
{
    /// <summary>Bekleyen işler sırayla; bitenler: tamamlanan / iptal edilen işler (yeniden eskiye).</summary>
    Task<List<UretimEmriDto>> ListeAsync(bool bitenler = false, CancellationToken ct = default);

    Task<UretimEmriDto> GetirAsync(int id, CancellationToken ct = default);

    /// <summary>Sıraya ekler; parçalar yetmezse hiçbir şey kaydedilmez (YetersizStokHatasi).</summary>
    Task<UretimEmriDto> EkleAsync(UretimEmriIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>İşten adet kadar (verilmezse kalanın hepsi) üretir. Siparişe bağlıysa üretilenler o kaleme ayrılır.</summary>
    Task<UretimSonucuDto> UretAsync(int id, EmirUretIstek istek, int kullaniciId, CancellationToken ct = default);

    /// <summary>Bekleyen işi kapatır; ayrılan parçalar serbest kalır. Bir kısmı üretildiyse iş o adetle tamamlanır.</summary>
    Task<UretimEmriDto> IptalAsync(int id, int kullaniciId, CancellationToken ct = default);

    /// <summary>Sırada bir yukarı (-1) ya da aşağı (+1) taşır.</summary>
    Task<List<UretimEmriDto>> TasiAsync(int id, int yon, CancellationToken ct = default);

    Task<UretimEmriDto> PlanlaAsync(int id, DateOnly tarih, CancellationToken ct = default);
}
