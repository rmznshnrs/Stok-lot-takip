using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

[ApiController]
[Route("api/parcalar")]
public class ParcalarController(IStokServisi stok, ITanimServisi tanim) : ControllerBase
{
    [HttpGet]
    public Task<List<ParcaStokDto>> Listele([FromQuery] string? arama, CancellationToken ct) => stok.ParcaStoklariAsync(arama, ct);

    [HttpGet("bul")]
    public Task<ParcaOzetDto> Bul([FromQuery] string metin, CancellationToken ct) => stok.ParcaBulAsync(metin, ct);

    [HttpPost]
    public async Task<ActionResult<ParcaOzetDto>> Olustur(ParcaIstek istek, CancellationToken ct)
    {
        var p = await tanim.ParcaOlusturAsync(istek, ct);
        return Created($"/api/parcalar/{p.Id}", p);
    }

    [HttpPut("{id:int}")]
    public Task<ParcaOzetDto> Guncelle(int id, ParcaIstek istek, CancellationToken ct) => tanim.ParcaGuncelleAsync(id, istek, ct);
}

[ApiController]
[Route("api/urunler")]
public class UrunlerController(ITanimServisi tanim) : ControllerBase
{
    [HttpGet]
    public Task<List<UrunListeDto>> Listele([FromQuery] string? arama, CancellationToken ct) => tanim.UrunleriListeleAsync(arama, ct);

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UrunDetayDto>> Getir(int id, CancellationToken ct) =>
        await tanim.UrunGetirAsync(id, ct) is { } u ? u : NotFound();

    [HttpPost]
    public async Task<ActionResult<UrunDetayDto>> Olustur(UrunIstek istek, CancellationToken ct)
    {
        var u = await tanim.UrunOlusturAsync(istek, ct);
        return Created($"/api/urunler/{u.Id}", u);
    }

    [HttpPut("{id:int}")]
    public Task<UrunDetayDto> Guncelle(int id, UrunIstek istek, CancellationToken ct) => tanim.UrunGuncelleAsync(id, istek, ct);

    [HttpPut("{id:int}/agac")]
    public Task<UrunDetayDto> AgacKaydet(int id, IReadOnlyList<UrunAgaciSatiriIstek> satirlar, CancellationToken ct) =>
        tanim.UrunAgaciKaydetAsync(id, satirlar, ct);
}

[ApiController]
[Route("api")]
public class ListelerController(ITanimServisi tanim) : ControllerBase
{
    [HttpGet("tedarikciler")]
    public Task<List<IletisimDto>> Tedarikciler(CancellationToken ct) => tanim.TedarikcilerAsync(ct);

    [HttpGet("musteriler")]
    public Task<List<IletisimDto>> Musteriler(CancellationToken ct) => tanim.MusterilerAsync(ct);

    [HttpPost("musteriler")]
    public Task<IletisimDto> MusteriOlustur(IletisimIstek istek, CancellationToken ct) => tanim.MusteriKaydetAsync(null, istek, ct);

    [HttpPut("musteriler/{id:int}")]
    public Task<IletisimDto> MusteriGuncelle(int id, IletisimIstek istek, CancellationToken ct) => tanim.MusteriKaydetAsync(id, istek, ct);

    [HttpPost("tedarikciler")]
    public Task<IletisimDto> TedarikciOlustur(IletisimIstek istek, CancellationToken ct) => tanim.TedarikciKaydetAsync(null, istek, ct);

    [HttpPut("tedarikciler/{id:int}")]
    public Task<IletisimDto> TedarikciGuncelle(int id, IletisimIstek istek, CancellationToken ct) => tanim.TedarikciKaydetAsync(id, istek, ct);
}
