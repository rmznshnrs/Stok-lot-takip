using LotTakip.Api.Kimlik;
using LotTakip.Business;
using LotTakip.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotTakip.Api.Controllers;

/// <summary>Giriş ve oturumdaki kullanıcı.</summary>
[ApiController]
[Route("api/kimlik")]
public class KimlikController(IKullaniciServisi kullanicilar, TokenUretici tokenUretici) : ControllerBase
{
    /// <summary>Kullanıcı adı ve şifreyle giriş; token döner. Diğer isteklerde "Authorization: Bearer {token}".</summary>
    [HttpPost("giris")]
    [AllowAnonymous]
    [ProducesResponseType<GirisSonucuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<GirisSonucuDto>> Giris(GirisIstek istek, CancellationToken ct)
    {
        var k = await kullanicilar.DogrulaAsync(istek.KullaniciAdi, istek.Sifre, ct);
        if (k is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Giriş başarısız",
                           detail: "Kullanıcı adı veya şifre hatalı.");
        return tokenUretici.Uret(k);
    }

    /// <summary>Giriş yapmış kullanıcının bilgileri.</summary>
    [HttpGet("ben")]
    public async Task<ActionResult<KullaniciDto>> Ben(CancellationToken ct) =>
        await kullanicilar.GetirAsync(User.KullaniciId(), ct) is { } k ? k : Unauthorized();

    /// <summary>Kendi şifresini değiştirir.</summary>
    [HttpPost("sifre")]
    public async Task<IActionResult> SifreDegistir(SifreDegistirIstek istek, CancellationToken ct)
    {
        await kullanicilar.SifreDegistirAsync(User.KullaniciId(), istek, ct);
        return NoContent();
    }
}

/// <summary>Kullanıcı yönetimi (yalnız Admin).</summary>
[ApiController]
[Route("api/kullanicilar")]
[Authorize(Policy = Politikalar.Admin)]
public class KullanicilarController(IKullaniciServisi kullanicilar) : ControllerBase
{
    [HttpGet]
    public Task<List<KullaniciDto>> Listele(CancellationToken ct) => kullanicilar.ListeleAsync(User.KullaniciId(), ct);

    [HttpPost]
    public async Task<ActionResult<KullaniciDto>> Olustur(KullaniciOlusturIstek istek, CancellationToken ct)
    {
        var k = await kullanicilar.OlusturAsync(istek, User.KullaniciId(), ct);
        return Created($"/api/kullanicilar/{k.Id}", k);
    }

    [HttpPut("{id:int}")]
    public Task<KullaniciDto> Guncelle(int id, KullaniciGuncelleIstek istek, CancellationToken ct) =>
        kullanicilar.GuncelleAsync(id, istek, User.KullaniciId(), ct);
}
