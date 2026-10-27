using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace PresentationLayer.Controllers;

/// <summary>Kayıt ol (yönetici onaylı), şifremi unuttum (e-postayla) ve kendi e-postası.</summary>
[ApiController]
[Route("api/hesap")]
public class HesapController(IHesapServisi hesap, IOptions<PostaAyarlari> posta) : ControllerBase
{
    [HttpPost("kayit")]
    [AllowAnonymous]
    public async Task<ActionResult<OnayBekleyenDto>> Kayit(KayitIstek istek, CancellationToken ct) =>
        await hesap.KayitAsync(istek, ct);

    /// <summary>Her durumda 204 döner; hesap varsa e-postasına sıfırlama bağlantısı gider.</summary>
    [HttpPost("sifre-unuttum")]
    [AllowAnonymous]
    public async Task<IActionResult> SifreUnuttum(SifreUnuttumIstek istek, CancellationToken ct)
    {
        await hesap.SifreUnuttumAsync(istek.KullaniciAdiVeyaEposta, posta.Value.WebAdresi, ct);
        return NoContent();
    }

    [HttpPost("sifre-sifirla")]
    [AllowAnonymous]
    public async Task<IActionResult> SifreSifirla(SifreSifirlaIstek istek, CancellationToken ct)
    {
        await hesap.SifreSifirlaAsync(istek, ct);
        return NoContent();
    }

    [HttpGet("eposta")]
    public Task<EpostaDto> Eposta(CancellationToken ct) => hesap.EpostaGetirAsync(User.KullaniciId(), ct);

    [HttpPut("eposta")]
    public Task<EpostaDto> EpostaAyarla(EpostaIstek istek, CancellationToken ct) =>
        hesap.EpostaAyarlaAsync(User.KullaniciId(), istek.Eposta, ct);

    [HttpGet("onay-bekleyenler")]
    [Authorize(Policy = Politikalar.Admin)]
    public Task<List<OnayBekleyenDto>> OnayBekleyenler(CancellationToken ct) =>
        hesap.OnayBekleyenlerAsync(User.KullaniciId(), ct);

    [HttpPost("{id:int}/onayla")]
    [Authorize(Policy = Politikalar.Admin)]
    public async Task<IActionResult> Onayla(int id, CancellationToken ct)
    {
        await hesap.OnaylaAsync(id, User.KullaniciId(), ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = Politikalar.Admin)]
    public async Task<IActionResult> Reddet(int id, CancellationToken ct)
    {
        await hesap.ReddetAsync(id, User.KullaniciId(), ct);
        return NoContent();
    }
}
