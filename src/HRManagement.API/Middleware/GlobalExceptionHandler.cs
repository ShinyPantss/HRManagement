using HRManagement.API.Models;
using Microsoft.AspNetCore.Diagnostics;

namespace HRManagement.API.Middleware;

/// <summary>
/// Tüm işlenmemiş exception'ları tek yerde BaseResponse'a çevirir (Gereksinim 6.3).
/// İki ayrı hata kaynağı var, ikisi de 400 döner:
///   • FluentValidation.ValidationException → ValidationBehavior'dan gelen INPUT hataları
///     (birden fazla alan hatası içerebilir, hepsi birleştirilir)
///   • DataAnnotations.ValidationException  → handler'daki İŞ KURALI reddi
///     (ör. "Bu e-posta zaten kullanılıyor")
/// Beklenmeyen her şey → 500.
/// Başarı ve hata AYNI zarfı kullanır; istemci (Refit) tek tip deserialize eder.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, message) = exception switch
        {
            FluentValidation.ValidationException validationException =>
                (StatusCodes.Status400BadRequest,
                 string.Join(" ", validationException.Errors.Select(error => error.ErrorMessage))),

            System.ComponentModel.DataAnnotations.ValidationException businessRuleException =>
                (StatusCodes.Status400BadRequest, businessRuleException.Message),

            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu.")
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            BaseResponse<object>.Fail(message), cancellationToken);

        return true;
    }
}
