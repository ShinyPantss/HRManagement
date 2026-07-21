using System.ComponentModel.DataAnnotations;
using HRManagement.API.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace HRManagement.API.Middleware;

/// <summary>
/// Tüm işlenmemiş exception'ları tek yerde BaseResponse'a çevirir (Gereksinim 6.3).
/// Handler'lardan gelen ValidationException → 400; beklenmeyen her şey → 500.
/// ÖNEMLİ: Başarı ve hata AYNI zarfı kullanır ({IsSuccess, Message, Data}); böylece
/// istemci (WebUI/Refit) tek bir tip deserialize eder.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu.")
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            BaseResponse<object>.Fail(message), cancellationToken);

        return true;
    }
}
