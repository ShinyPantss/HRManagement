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
///
/// Loglama YALNIZCA 500 dalında yapılır: istemciye giden mesaj bilinçli olarak
/// içi boştur ("Beklenmeyen bir hata oluştu."), dolayısıyla iz burada düşmezse
/// hata hiçbir yere düşmez. 400'ler ise normal akıştır (kullanıcı yanlış veri
/// girdi) — loglanırsa gürültü olur ve gerçek hatalar arasında kaybolur.
/// </summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

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

        if (status == StatusCodes.Status500InternalServerError)
        {
            // Exception nesnesi İLK parametre olarak verilir: yığın izi (stack
            // trace) ve iç exception'lar ancak böyle loglanır.
            _logger.LogError(
                exception,
                "İşlenmemiş hata: {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            BaseResponse<object>.Fail(message), cancellationToken);

        return true;
    }
}
