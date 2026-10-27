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

    /// <summary>Hatalı parça kaydı (yalnız Admin): stoktan düşülerek ya da yalnız sayı olarak.</summary>
    [HttpPost("hatali")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<StokDuzeltmeSonucuDto> Hatali(StokHataliIstek istek, CancellationToken ct) =>
        duzeltme.HataliAsync(istek, User.KullaniciId(), ct);
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

    /// <summary>Üretilen seri numaraları (yeniden eskiye): satış durumu, geri çağrılan lotlar.</summary>
    [HttpGet("gecmis")]
    public Task<List<UretimGecmisiDto>> Gecmis([FromQuery] string? arama, [FromQuery] int? urunId, CancellationToken ct) =>
        uretim.UretimGecmisiAsync(arama, urunId, ct: ct);

    /// <summary>Depodaki seri numaralarını hurdaya / kayba ayırır (yalnız Admin, açıklama zorunlu).</summary>
    [HttpPost("hurda")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<UretimHurdaSonucuDto> Hurda(UretimHurdaIstek istek, CancellationToken ct) =>
        uretim.HurdayaAyirAsync(istek, User.KullaniciId(), ct);
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

    /// <summary>Satışı kaydeder. Sorunlu lot içeren ürün varsa GeriCagrilanOnayi=true gerekir (yoksa 409 ve "uyarilar").</summary>
    [HttpPost]
    public async Task<ActionResult<SatisSonucuDto>> Sat(SatisIstek istek, CancellationToken ct)
    {
        var s = await satis.SatAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/satis/gecmis?arama={Uri.EscapeDataString(s.SeriNolar[0])}", s);
    }

    [HttpGet("gecmis")]
    public Task<List<SatisGecmisiDto>> Gecmis([FromQuery] string? arama, CancellationToken ct) => satis.SatisGecmisiAsync(arama, ct: ct);

    /// <summary>Satıştan iade (yalnız Admin, açıklama zorunlu). Ürün depoya döner.</summary>
    [HttpPost("{id:int}/iade")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<SatisIadeSonucuDto> Iade(int id, SatisIadeIstek istek, CancellationToken ct) =>
        satis.IadeAlAsync(id, istek, User.KullaniciId(), ct);
}

/// <summary>Kim, ne zaman, ne yaptı (yalnız Admin).</summary>
[ApiController]
[Route("api/islemler")]
[Authorize(Policy = Politikalar.Admin)]
public class IslemlerController(IIslemGecmisiServisi islemler) : ControllerBase
{
    [HttpGet]
    public Task<List<IslemDto>> Liste([FromQuery] string? tur, [FromQuery] int? kullaniciId, [FromQuery] DateTimeOffset? once,
                                      [FromQuery] int adet = 50, CancellationToken ct = default) =>
        islemler.IslemlerAsync(User.KullaniciId(), tur, kullaniciId, once, adet, ct);
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

    /// <summary>Lotu hatalı işaretler / hatayı kaldırır (yalnız Admin). Yalnız seçilen ürünler geri çağrılır.</summary>
    [HttpPost("lot/{lotNo}/sorun")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<LotSorunSonucuDto> Sorun(string lotNo, LotSorunIstek istek, CancellationToken ct) =>
        izleme.LotSorunAsync(lotNo, istek, User.KullaniciId(), ct);
}

/// <summary>Ürün (seri no) geri çağırma. Liste herkese, işlemler yalnız Admin.</summary>
[ApiController]
[Route("api/geri-cagirma")]
public class GeriCagirmaController(IGeriCagirmaServisi geri) : ControllerBase
{
    [HttpGet]
    public Task<List<GeriCagirmaDto>> Liste([FromQuery] bool acik = false, [FromQuery] string? arama = null, CancellationToken ct = default) =>
        geri.ListeAsync(acik, arama, ct);

    [HttpPost]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<List<GeriCagirmaDto>> GeriCagir(GeriCagirmaIstek istek, CancellationToken ct) =>
        geri.GeriCagirAsync(istek, User.KullaniciId(), ct);

    /// <summary>Müşterideki ürün geri alındı: iade yapılır, ürün depoya döner (satışa kapalı kalır).</summary>
    [HttpPost("{id:int}/geri-al")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> GeriAl(int id, CancellationToken ct) => geri.GeriAlAsync(id, User.KullaniciId(), ct);

    /// <summary>Depodaki ürün yeniden satışa açılır; geri çağırma kapanır.</summary>
    [HttpPost("{id:int}/satisa-ac")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> SatisaAc(int id, CancellationToken ct) => geri.SatisaAcAsync(id, User.KullaniciId(), ct);

    /// <summary>Sorun yok: ürün müşteriye geri verilir; geri çağırma kapanır.</summary>
    [HttpPost("{id:int}/musteriye-ver")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> MusteriyeVer(int id, CancellationToken ct) => geri.MusteriyeVerAsync(id, User.KullaniciId(), ct);
}

/// <summary>Ürün lotları (aynı anda üretilenler). Liste herkese, hata işareti yalnız Admin.</summary>
[ApiController]
[Route("api/urun-lot")]
public class UrunLotController(IUrunLotServisi lotlar) : ControllerBase
{
    [HttpGet]
    public Task<List<UrunLotDto>> Liste([FromQuery] int? urunId, CancellationToken ct) => lotlar.ListeAsync(urunId, ct);

    [HttpPost("{lotNo}/sorun")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<UrunLotSorunSonucuDto> Sorun(string lotNo, UrunLotSorunIstek istek, CancellationToken ct) =>
        lotlar.SorunAsync(lotNo, istek, User.KullaniciId(), ct);
}
