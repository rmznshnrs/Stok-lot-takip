using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LotTakip.Api.Controllers;

/// <summary>API'nin ayakta olduğunu gösterir (giriş gerektirmez).</summary>
[ApiController]
[Route("api/saglik")]
[AllowAnonymous]
public class SaglikController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { durum = "çalışıyor", zaman = DateTimeOffset.Now });
}
