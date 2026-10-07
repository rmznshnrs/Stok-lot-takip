using LotTakip.Business;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace LotTakip.Api;

/// <summary>
/// İş kuralı hatalarını HTTP yanıtına çevirir (ProblemDetails). Mesaj Türkçe olarak "detail"de;
/// yetersiz stokta "eksikler", geri çağrılan lot onayında "uyarilar" ek alan olarak döner.
/// Beklenmeyen hatalar burada işlenmez (500, ayrıntı gösterilmez).
/// </summary>
public class HataIsleyici(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception hata, CancellationToken ct)
    {
        if (hata is not IsKuraliHatasi kural)
            return false;

        var (durum, baslik) = kural switch
        {
            BulunamadiHatasi => (StatusCodes.Status404NotFound, "Bulunamadı"),
            YetkiHatasi => (StatusCodes.Status403Forbidden, "Yetki yok"),
            YetersizStokHatasi => (StatusCodes.Status409Conflict, "Yetersiz stok"),
            GeriCagrilanLotOnayiGerekliHatasi => (StatusCodes.Status409Conflict, "Onay gerekli"),
            _ => (StatusCodes.Status400BadRequest, "İşlem yapılamadı"),
        };
        var problem = new ProblemDetails { Status = durum, Title = baslik, Detail = kural.Message };
        switch (kural)
        {
            case YetersizStokHatasi y:
                problem.Extensions["eksikler"] = y.Eksikler;
                break;
            case GeriCagrilanLotOnayiGerekliHatasi g:
                problem.Extensions["uyarilar"] = g.Uyarilar;
                break;
        }

        http.Response.StatusCode = durum;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http, ProblemDetails = problem, Exception = hata,
        });
    }
}
