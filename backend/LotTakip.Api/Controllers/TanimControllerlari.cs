using LotTakip.Business;
using LotTakip.Shared;
using Microsoft.AspNetCore.Mvc;

namespace LotTakip.Api.Controllers;

/// <summary>Parça tanımları ve stok bilgileri.</summary>
[ApiController]
[Route("api/parcalar")]
public class ParcalarController(IStokServisi stok, ITanimServisi tanim) : ControllerBase
{
    /// <summary>Parçalar: toplam ve kullanılabilir stok, lot sayısı, kullanıldığı ürünler. Arama kod veya adda.</summary>
    [HttpGet]
    public Task<List<ParcaStokDto>> Listele([FromQuery] string? arama, CancellationToken ct) => stok.ParcaStoklariAsync(arama, ct);

    /// <summary>Kod, ad veya tek kısmi eşleşmeyle parça bulur (mal girişi, ürün ağacına ekleme).</summary>
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

/// <summary>Ürün tanımları ve ürün ağaçları.</summary>
[ApiController]
[Route("api/urunler")]
public class UrunlerController(ITanimServisi tanim) : ControllerBase
{
    [HttpGet]
    public Task<List<UrunListeDto>> Listele([FromQuery] string? arama, CancellationToken ct) => tanim.UrunleriListeleAsync(arama, ct);

    /// <summary>Ürün ve ürün ağacı (her satırda parçanın kullanılabilir stoğu).</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<UrunDetayDto>> Getir(int id, CancellationToken ct) =>
        await tanim.UrunGetirAsync(id, ct) is { } u ? u : NotFound();

    [HttpPost]
    public async Task<ActionResult<UrunDetayDto>> Olustur(UrunIstek istek, CancellationToken ct)
    {
        var u = await tanim.UrunOlusturAsync(istek, ct);
        return Created($"/api/urunler/{u.Id}", u);
    }

    /// <summary>Ad, kod, seri no öneki. Üretimi olan ürünün öneki değişmez.</summary>
    [HttpPut("{id:int}")]
    public Task<UrunDetayDto> Guncelle(int id, UrunIstek istek, CancellationToken ct) => tanim.UrunGuncelleAsync(id, istek, ct);

    /// <summary>Ürün ağacını verilen satırlarla değiştirir (listede olmayan satırlar kaldırılır).</summary>
    [HttpPut("{id:int}/agac")]
    public Task<UrunDetayDto> AgacKaydet(int id, IReadOnlyList<UrunAgaciSatiriIstek> satirlar, CancellationToken ct) =>
        tanim.UrunAgaciKaydetAsync(id, satirlar, ct);
}

/// <summary>Seçim listeleri.</summary>
[ApiController]
[Route("api")]
public class ListelerController(ITanimServisi tanim) : ControllerBase
{
    [HttpGet("tedarikciler")]
    public Task<List<IletisimDto>> Tedarikciler(CancellationToken ct) => tanim.TedarikcilerAsync(ct);

    [HttpGet("musteriler")]
    public Task<List<IletisimDto>> Musteriler(CancellationToken ct) => tanim.MusterilerAsync(ct);
}
