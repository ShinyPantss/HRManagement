namespace HRManagement.API.Models;

/// <summary>
/// Tüm API yanıtlarının ortak zarfı. İstemci her zaman aynı şekli parse eder:
/// önce IsSuccess'e bakar, başarılıysa Data'yı kullanır, değilse Message'ı gösterir.
/// </summary>
public class BaseResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static BaseResponse<T> Success(T data, string? message = null) =>
        new() { IsSuccess = true, Data = data, Message = message };

    public static BaseResponse<T> Fail(string message) =>
        new() { IsSuccess = false, Message = message };
}
