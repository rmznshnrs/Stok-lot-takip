namespace BusinessLayer.Abstract;

public interface IStokServisi
{
    Task<List<ParcaStokDto>> ParcaStoklariAsync(string? arama = null, CancellationToken ct = default);

    Task<List<StokGrubuDto>> StokDurumuAsync(string? arama = null, string? parcaKod = null, bool bitenler = false,
                                             CancellationToken ct = default);

    Task<MalGirisiSonucuDto> MalGirisiAsync(MalGirisiIstek istek, int kullaniciId, CancellationToken ct = default);

    Task<ParcaOzetDto> ParcaBulAsync(string metin, CancellationToken ct = default);
}
