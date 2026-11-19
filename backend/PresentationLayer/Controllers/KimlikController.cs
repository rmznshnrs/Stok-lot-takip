using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PresentationLayer.Controllers;

[ApiController]
[Route("api/kimlik")]
public class KimlikController(IKullaniciServisi kullanicilar, IHesapServisi hesap, TokenUretici tokenUretici) : ControllerBase
{
    [HttpPost("giris")]
    [AllowAnonymous]
    [ProducesResponseType<GirisSonucuDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GirisSonucuDto>> Giris(GirisIstek istek, CancellationToken ct)
    {
        var k = await kullanicilar.DogrulaAsync(istek.KullaniciAdi, istek.Sifre, ct);
        if (k is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Giriş başarısız",
                           detail: "Kullanıcı adı veya şifre hatalı.");
        if (!await hesap.OnayliMiAsync(k.Id, ct))
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Onay bekleniyor",
                           detail: "Hesabınız henüz yönetici tarafından onaylanmadı.");
        return tokenUretici.Uret(k);
    }

    [HttpGet("ben")]
    public async Task<ActionResult<KullaniciDto>> Ben(CancellationToken ct) =>
        await kullanicilar.GetirAsync(User.KullaniciId(), ct) is { } k ? k : Unauthorized();

    [HttpPost("sifre")]
    public async Task<IActionResult> SifreDegistir(SifreDegistirIstek istek, CancellationToken ct)
    {
        await kullanicilar.SifreDegistirAsync(User.KullaniciId(), istek, ct);
        return NoContent();
    }
}

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

    [HttpDelete("{id:int}")]
    public Task<KullaniciSilSonucuDto> Sil(int id, CancellationToken ct) => kullanicilar.SilAsync(id, User.KullaniciId(), ct);

    [HttpPost("{id:int}/etkinlestir")]
    public Task<KullaniciDto> Etkinlestir(int id, CancellationToken ct) => kullanicilar.EtkinlestirAsync(id, User.KullaniciId(), ct);
}
