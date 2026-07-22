using HRManagement.API.Models;

namespace HRManagement.API.Middleware;

public static class StatusCodeResponseExtensions
{
    /// <summary>
    /// Gövdesiz hata yanıtlarını BaseResponse zarfına sokar.
    ///
    /// Neden gerekli: 401/403/404/405 gibi durumlarda ASP.NET Core yalnızca durum kodu
    /// döner, gövde BOŞTUR (0 bayt). İstemcimiz (Refit) her yanıtı BaseResponse olarak
    /// okumaya çalıştığı için boş gövde o sözleşmeyi bozar — WebUI'da IsSuccess/Message
    /// okunamaz hâle gelir. Burada tek biçimliliği geri kazandırıyoruz.
    ///
    /// Zaten gövde yazılmış yanıtlara dokunmaz: StatusCodePages middleware'i yalnızca
    /// Content-Type ve Content-Length boşsa devreye girer. Yani GlobalExceptionHandler'ın
    /// veya controller'ın ürettiği BaseResponse'lar olduğu gibi kalır.
    /// </summary>
    public static IApplicationBuilder UseBaseResponseStatusCodes(this IApplicationBuilder app)
    {
        return app.UseStatusCodePages(async statusCodeContext =>
        {
            var response = statusCodeContext.HttpContext.Response;

            var message = response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => "Giriş yapmanız gerekiyor.",
                StatusCodes.Status403Forbidden => "Bu işlem için yetkiniz yok.",
                StatusCodes.Status404NotFound => "İstenen kaynak bulunamadı.",
                StatusCodes.Status405MethodNotAllowed => "Bu adres için geçersiz HTTP metodu.",
                _ => "İstek işlenemedi."
            };

            await response.WriteAsJsonAsync(BaseResponse<object>.Fail(message));
        });
    }
}
