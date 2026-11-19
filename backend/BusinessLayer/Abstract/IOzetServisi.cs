namespace BusinessLayer.Abstract;

public interface IOzetServisi
{
    Task<AnaSayfaOzetiDto> AnaSayfaAsync(CancellationToken ct = default);

    Task<List<TakvimGunuDto>> TakvimAsync(DateOnly baslangic, DateOnly bitis, CancellationToken ct = default);

    Task<TakvimPlaniDto> PlanEkleAsync(TakvimPlaniIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<TakvimPlaniDto> PlanTamamAsync(int id, bool tamamlandi, CancellationToken ct = default);

    Task PlanSilAsync(int id, CancellationToken ct = default);
}
