using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

[ApiController]
[Route("api/stok")]
public class StokController(IStokServisi stok, IStokDuzeltmeServisi duzeltme) : ControllerBase
{
    [HttpGet]
    public Task<List<StokGrubuDto>> Durum([FromQuery] string? arama, [FromQuery] string? parca,
                                          [FromQuery] bool bitenler = false, CancellationToken ct = default) =>
        stok.StokDurumuAsync(arama, parca, bitenler, ct);

    [HttpPost("giris")]
    public async Task<ActionResult<MalGirisiSonucuDto>> MalGirisi(MalGirisiIstek istek, CancellationToken ct)
    {
        var s = await stok.MalGirisiAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/izleme/lot/{Uri.EscapeDataString(s.Lot.LotNo)}", s);
    }

    [HttpPost("duzeltme")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<StokDuzeltmeSonucuDto> Duzelt(StokDuzeltmeIstek istek, CancellationToken ct) =>
        duzeltme.DuzeltAsync(istek, User.KullaniciId(), ct);

    [HttpPost("hatali")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<StokDuzeltmeSonucuDto> Hatali(StokHataliIstek istek, CancellationToken ct) =>
        duzeltme.HataliAsync(istek, User.KullaniciId(), ct);

    [HttpDelete("lot/{id:int}")]
    [Authorize(Policy = Politikalar.Admin)]
    public async Task<IActionResult> LotSil(int id, CancellationToken ct)
    {
        await stok.LotSilAsync(id, User.KullaniciId(), ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/uretim")]
public class UretimController(IUretimServisi uretim, IUretimSirasiServisi sira) : ControllerBase
{
    [HttpGet("seri-onerisi")]
    public Task<List<string>> SeriOnerisi([FromQuery] int urunId, [FromQuery] int adet, CancellationToken ct) =>
        uretim.SonrakiSeriNolarAsync(urunId, adet, ct);

    [HttpGet("onizleme")]
    public Task<UretimOnizlemeDto> Onizle([FromQuery] int urunId, [FromQuery] int adet, CancellationToken ct) =>
        uretim.OnizleAsync(urunId, adet, ct);

    [HttpGet("sira")]
    public Task<List<UretimEmriDto>> Sira([FromQuery] bool bitenler, CancellationToken ct) => sira.ListeAsync(bitenler, ct);

    [HttpPost("sira")]
    public async Task<ActionResult<UretimEmriDto>> SirayaEkle(UretimEmriIstek istek, CancellationToken ct)
    {
        var e = await sira.EkleAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/uretim/sira/{e.Id}", e);
    }

    [HttpGet("sira/{id:int}")]
    public Task<UretimEmriDto> SiraGetir(int id, CancellationToken ct) => sira.GetirAsync(id, ct);

    [HttpPost("sira/{id:int}/uret")]
    public Task<UretimSonucuDto> SiradanUret(int id, EmirUretIstek istek, CancellationToken ct) =>
        sira.UretAsync(id, istek, User.KullaniciId(), ct);

    [HttpPost("sira/{id:int}/basla")]
    public Task<UretimEmriDto> SiraBasla(int id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] EmirBaslaIstek? istek,
        CancellationToken ct) => sira.BaslaAsync(id, ct, istek?.Adet);

    [HttpPost("sira/{id:int}/iptal")]
    public Task<UretimEmriDto> SiraIptal(int id, CancellationToken ct) => sira.IptalAsync(id, User.KullaniciId(), ct);

    [HttpPost("sira/{id:int}/tasi")]
    public Task<List<UretimEmriDto>> SiraTasi(int id, EmirTasiIstek istek, CancellationToken ct) => sira.TasiAsync(id, istek.Yon, ct);

    [HttpPost("sira/{id:int}/konum")]
    public Task<List<UretimEmriDto>> SiraKonum(int id, EmirKonumIstek istek, CancellationToken ct) =>
        sira.KonumaTasiAsync(id, istek.HedefId, ct);

    [HttpPut("sira/{id:int}/plan")]
    public Task<UretimEmriDto> SiraPlan(int id, EmirPlanIstek istek, CancellationToken ct) =>
        sira.PlanlaAsync(id, istek.PlanlananTarih, ct);

    [HttpGet("gecmis")]
    public Task<List<UretimGecmisiDto>> Gecmis([FromQuery] string? arama, [FromQuery] int? urunId, CancellationToken ct) =>
        uretim.UretimGecmisiAsync(arama, urunId, ct: ct);

    [HttpPost("hurda")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<UretimHurdaSonucuDto> Hurda(UretimHurdaIstek istek, CancellationToken ct) =>
        uretim.HurdayaAyirAsync(istek, User.KullaniciId(), ct);
}

[ApiController]
[Route("api/siparis")]
public class SiparisController(ISiparisServisi siparis) : ControllerBase
{
    [HttpGet]
    public Task<List<SiparisDto>> Liste([FromQuery] int? musteriId, [FromQuery] bool acik, CancellationToken ct) =>
        siparis.ListeAsync(musteriId, acik, ct);

    [HttpGet("{id:int}")]
    public Task<SiparisDto> Getir(int id, CancellationToken ct) => siparis.GetirAsync(id, ct);

    [HttpPut("{id:int}/siparis-no")]
    public Task<SiparisDto> SiparisNo(int id, SiparisNoIstek istek, CancellationToken ct) => siparis.SiparisNoAsync(id, istek, ct);

    [HttpGet("{id:int}/urunler")]
    public Task<List<SiparisUrunuDto>> Urunler(int id, CancellationToken ct) => siparis.UrunlerAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<SiparisDto>> Olustur(SiparisIstek istek, CancellationToken ct)
    {
        var s = await siparis.OlusturAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/siparis/{s.Id}", s);
    }

    [HttpPost("{id:int}/iptal")]
    public Task<SiparisDto> Iptal(int id, CancellationToken ct) => siparis.IptalAsync(id, User.KullaniciId(), ct);

    [HttpPost("kalem/{kalemId:int}/ayir")]
    public Task<SiparisDto> Ayir(int kalemId, SeriAyirIstek istek, CancellationToken ct) => siparis.SeriAyirAsync(kalemId, istek, ct, User.KullaniciId());

    [HttpDelete("kalem/{kalemId:int}/seri/{seriNo}")]
    public Task<SiparisDto> Birak(int kalemId, string seriNo, CancellationToken ct) => siparis.SeriBirakAsync(kalemId, seriNo, ct);

    [HttpGet("hazir")]
    public Task<List<HazirSeriDto>> Hazir([FromQuery] int urunId, CancellationToken ct) => siparis.HazirSerilerAsync(urunId, ct);
}

[ApiController]
[Route("api/sevkiyat")]
public class SevkiyatController(ISevkiyatServisi sevkiyat) : ControllerBase
{
    [HttpGet]
    public Task<List<SevkiyatDto>> Liste([FromQuery] string? arama, [FromQuery] string? durum, [FromQuery] int? musteriId,
                                         CancellationToken ct) =>
        sevkiyat.ListeAsync(arama, durum, musteriId, ct);

    [HttpGet("{id:int}")]
    public Task<SevkiyatDto> Getir(int id, CancellationToken ct) => sevkiyat.GetirAsync(id, ct);

    [HttpPost]
    public async Task<ActionResult<SevkiyatDto>> Olustur(SevkiyatIstek istek, CancellationToken ct)
    {
        var s = await sevkiyat.OlusturAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/sevkiyat/{s.Id}", s);
    }

    [HttpPut("{id:int}/kargo")]
    public Task<SevkiyatDto> Kargo(int id, KargoIstek istek, CancellationToken ct) => sevkiyat.KargoAsync(id, istek, ct);

    [HttpPut("{id:int}/no")]
    public Task<SevkiyatDto> No(int id, SevkiyatNoIstek istek, CancellationToken ct) => sevkiyat.NoAsync(id, istek, ct);

    [HttpPost("{id:int}/teslim")]
    public Task<SevkiyatDto> Teslim(int id, CancellationToken ct) => sevkiyat.TeslimAsync(id, ct);
}

[ApiController]
[Route("api/ozet")]
public class OzetController(IOzetServisi ozet, IRaporServisi rapor) : ControllerBase
{
    [HttpGet]
    public Task<AnaSayfaOzetiDto> AnaSayfa(CancellationToken ct) => ozet.AnaSayfaAsync(ct);

    [HttpGet("grafikler")]
    public Task<GrafikVerisiDto> Grafikler(CancellationToken ct) => ozet.GrafiklerAsync(ct);

    [HttpGet("takvim")]
    public Task<List<TakvimGunuDto>> Takvim([FromQuery] DateOnly baslangic, [FromQuery] DateOnly bitis, CancellationToken ct) =>
        ozet.TakvimAsync(baslangic, bitis, ct);

    [HttpPost("takvim/plan")]
    public Task<TakvimPlaniDto> PlanEkle(TakvimPlaniIstek istek, CancellationToken ct) => ozet.PlanEkleAsync(istek, User.KullaniciId(), ct);

    [HttpPost("takvim/plan/{id:int}/tamam")]
    public Task<TakvimPlaniDto> PlanTamam(int id, [FromQuery] bool tamamlandi = true, CancellationToken ct = default) =>
        ozet.PlanTamamAsync(id, tamamlandi, ct);

    [HttpDelete("takvim/plan/{id:int}")]
    public async Task<IActionResult> PlanSil(int id, CancellationToken ct)
    {
        await ozet.PlanSilAsync(id, ct);
        return NoContent();
    }

    [HttpGet("excel")]
    [Authorize(Policy = Politikalar.Admin)]
    public async Task<IActionResult> Excel([FromQuery] int ay, CancellationToken ct)
    {
        var (icerik, ad) = await rapor.ExcelAsync(ay, ct);
        return File(icerik, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad);
    }
}

[ApiController]
[Route("api/satis")]
public class SatisController(ISatisServisi satis) : ControllerBase
{
    [HttpGet("kontrol")]
    public Task<SatilabilirDto> Kontrol([FromQuery] string seriNo, CancellationToken ct) => satis.SatilabilirAsync(seriNo, ct);

    [HttpPost("uyarilar")]
    public Task<List<SatisUyarisiDto>> Uyarilar(SatisUyarilariIstek istek, CancellationToken ct) =>
        satis.SatisUyarilariAsync(istek.SeriNolar, ct);

    [HttpPost]
    public async Task<ActionResult<SatisSonucuDto>> Sat(SatisIstek istek, CancellationToken ct)
    {
        var s = await satis.SatAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/satis/gecmis?arama={Uri.EscapeDataString(s.SeriNolar[0])}", s);
    }

    [HttpGet("gecmis")]
    public Task<List<SatisGecmisiDto>> Gecmis([FromQuery] string? arama, [FromQuery] int? musteriId, CancellationToken ct) =>
        satis.SatisGecmisiAsync(arama, ct: ct, musteriId: musteriId);

    [HttpPost("{id:int}/iade")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<SatisIadeSonucuDto> Iade(int id, SatisIadeIstek istek, CancellationToken ct) =>
        satis.IadeAlAsync(id, istek, User.KullaniciId(), ct);
}

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

[ApiController]
[Route("api/izleme")]
public class IzlemeController(IIzlemeServisi izleme) : ControllerBase
{
    [HttpGet("lot/{lotNo}")]
    public async Task<ActionResult<LotIzDto>> Lot(string lotNo, CancellationToken ct) =>
        await izleme.LotIzleAsync(lotNo, ct) is { } iz ? iz : NotFound();

    [HttpGet("seri/{seriNo}")]
    public async Task<ActionResult<SeriIzDto>> Seri(string seriNo, CancellationToken ct) =>
        await izleme.SeriIzleAsync(seriNo, ct) is { } iz ? iz : NotFound();

    [HttpGet("ara")]
    public Task<NumaraAramaDto> Ara([FromQuery] string q, CancellationToken ct) => izleme.NumaraAraAsync(q, ct);

    [HttpGet("hareketler")]
    public Task<List<HareketDto>> Hareketler([FromQuery] int adet = 8, CancellationToken ct = default) =>
        izleme.SonHareketlerAsync(Math.Clamp(adet, 1, 100), ct);

    [HttpPost("lot/{lotNo}/sorun")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<LotSorunSonucuDto> Sorun(string lotNo, LotSorunIstek istek, CancellationToken ct) =>
        izleme.LotSorunAsync(lotNo, istek, User.KullaniciId(), ct);
}

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

    [HttpPost("{id:int}/geri-al")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> GeriAl(int id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] KargoIstek? kargo,
                                       CancellationToken ct) => geri.GeriAlAsync(id, User.KullaniciId(), ct, kargo);

    [HttpPost("{id:int}/satisa-ac")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> SatisaAc(int id, CancellationToken ct) => geri.SatisaAcAsync(id, User.KullaniciId(), ct);

    [HttpPost("{id:int}/musteriye-ver")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> MusteriyeVer(int id, [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] KargoIstek? kargo,
                                             CancellationToken ct) => geri.MusteriyeVerAsync(id, User.KullaniciId(), ct, kargo);

    [HttpPost("{id:int}/bilgi-verildi")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> BilgiVerildi(int id, CancellationToken ct) => geri.BilgiVerildiAsync(id, User.KullaniciId(), ct);

    [HttpPost("{id:int}/hurda")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<GeriCagirmaDto> Hurda(int id, GeriCagirmaHurdaIstek istek, CancellationToken ct) =>
        geri.HurdayaAyirAsync(id, istek, User.KullaniciId(), ct);
}

[ApiController]
[Route("api/kontrol")]
public class KontrolController(IKontrolServisi kontrol) : ControllerBase
{
    [HttpGet("numara")]
    public Task<NumaraKontrolDto> Numara([FromQuery] string tur, [FromQuery] string deger, [FromQuery] int? haric,
                                         CancellationToken ct) => kontrol.NumaraAsync(tur, deger, haric, ct);
}
