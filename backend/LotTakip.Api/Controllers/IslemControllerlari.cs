using LotTakip.Api.Kimlik;
using LotTakip.Business;
using LotTakip.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotTakip.Api.Controllers;

/// <summary>Stok listesi, mal girişi, stok düzeltme.</summary>
[ApiController]
[Route("api/stok")]
public class StokController(IStokServisi stok, IStokDuzeltmeServisi duzeltme) : ControllerBase
{
    /// <summary>Parça bazında lotlar (FIFO sırasıyla) ve sıradaki lot. bitenler=true kalanı 0 olanları da gösterir.</summary>
    [HttpGet]
    public Task<List<StokGrubuDto>> Durum([FromQuery] string? arama, [FromQuery] string? parca,
                                          [FromQuery] bool bitenler = false, CancellationToken ct = default) =>
        stok.StokDurumuAsync(arama, parca, bitenler, ct);

    /// <summary>Mal girişi: yeni lot. Tedarikçi adı yoksa tedarikçi de oluşturulur.</summary>
    [HttpPost("giris")]
    public async Task<ActionResult<MalGirisiSonucuDto>> MalGirisi(MalGirisiIstek istek, CancellationToken ct)
    {
        var s = await stok.MalGirisiAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/izleme/lot/{Uri.EscapeDataString(s.Lot.LotNo)}", s);
    }

    /// <summary>Stok düzeltme (yalnız Admin): sayım farkı, fire, diğer; açıklama zorunlu.</summary>
    [HttpPost("duzeltme")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<StokDuzeltmeSonucuDto> Duzelt(StokDuzeltmeIstek istek, CancellationToken ct) =>
        duzeltme.DuzeltAsync(istek, User.KullaniciId(), ct);
}

/// <summary>Üretim: önizleme ve kayıt.</summary>
[ApiController]
[Route("api/uretim")]
public class UretimController(IUretimServisi uretim) : ControllerBase
{
    /// <summary>Kaydetmeden: hangi lottan kaç parça düşüleceği, eksikler, verilecek seri numaraları.</summary>
    [HttpGet("onizleme")]
    public Task<UretimOnizlemeDto> Onizle([FromQuery] int urunId, [FromQuery] int adet, CancellationToken ct) =>
        uretim.OnizleAsync(urunId, adet, ct);

    /// <summary>Üretimi kaydeder. Stok yetmezse 409 ve "eksikler".</summary>
    [HttpPost]
    public Task<UretimSonucuDto> Uret(UretimIstek istek, CancellationToken ct) =>
        uretim.UretAsync(istek.UrunId, istek.Adet, User.KullaniciId(), ct: ct);
}

/// <summary>Satış.</summary>
[ApiController]
[Route("api/satis")]
public class SatisController(ISatisServisi satis) : ControllerBase
{
    /// <summary>Seri no satışa eklenebilir mi (var mı, satılmış mı, geri çağrılan lot içeriyor mu).</summary>
    [HttpGet("kontrol")]
    public Task<SatilabilirDto> Kontrol([FromQuery] string seriNo, CancellationToken ct) => satis.SatilabilirAsync(seriNo, ct);

    /// <summary>Geri çağrılan lot içeren seri numaraları (kaydetmeden).</summary>
    [HttpPost("uyarilar")]
    public Task<List<SatisUyarisiDto>> Uyarilar(SatisUyarilariIstek istek, CancellationToken ct) =>
        satis.SatisUyarilariAsync(istek.SeriNolar, ct);

    /// <summary>Satışı kaydeder. Geri çağrılan lot içeren ürün varsa GeriCagrilanOnayi=true gerekir (yoksa 409 ve "uyarilar").</summary>
    [HttpPost]
    public async Task<ActionResult<SatisSonucuDto>> Sat(SatisIstek istek, CancellationToken ct)
    {
        var s = await satis.SatAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/satis/gecmis?arama={Uri.EscapeDataString(s.SeriNolar[0])}", s);
    }

    [HttpGet("gecmis")]
    public Task<List<SatisGecmisiDto>> Gecmis([FromQuery] string? arama, CancellationToken ct) => satis.SatisGecmisiAsync(arama, ct: ct);
}

/// <summary>İzlenebilirlik, arama, son hareketler, lot geri çağırma.</summary>
[ApiController]
[Route("api/izleme")]
public class IzlemeController(IIzlemeServisi izleme) : ControllerBase
{
    /// <summary>Lot → kullanıldığı seri numaraları → müşteriler.</summary>
    [HttpGet("lot/{lotNo}")]
    public async Task<ActionResult<LotIzDto>> Lot(string lotNo, CancellationToken ct) =>
        await izleme.LotIzleAsync(lotNo, ct) is { } iz ? iz : NotFound();

    /// <summary>Seri no → içindeki parçalar ve lotları, satış bilgisi.</summary>
    [HttpGet("seri/{seriNo}")]
    public async Task<ActionResult<SeriIzDto>> Seri(string seriNo, CancellationToken ct) =>
        await izleme.SeriIzleAsync(seriNo, ct) is { } iz ? iz : NotFound();

    /// <summary>Numaranın lot, seri no veya parça kodu olduğunu algılar; yoksa adaylar.</summary>
    [HttpGet("ara")]
    public Task<NumaraAramaDto> Ara([FromQuery] string q, CancellationToken ct) => izleme.NumaraAraAsync(q, ct);

    [HttpGet("hareketler")]
    public Task<List<HareketDto>> Hareketler([FromQuery] int adet = 8, CancellationToken ct = default) =>
        izleme.SonHareketlerAsync(Math.Clamp(adet, 1, 100), ct);

    /// <summary>Lotu geri çağırır veya geri çağırmayı kaldırır (yalnız Admin).</summary>
    [HttpPost("lot/{lotNo}/geri-cagir")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<LotDto> GeriCagir(string lotNo, GeriCagirIstek istek, CancellationToken ct) =>
        izleme.LotGeriCagirAsync(lotNo, istek.GeriCagir, User.KullaniciId(), ct);
}
