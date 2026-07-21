namespace HRManagement.WebUI.Models.Api;

/// <summary>
/// API'nin döndüğü zarfın WebUI tarafındaki karşılığı. Paylaşılan Contracts projesi
/// olmadığı için bu sınıf API'dekinin kopyasıdır — JSON şekli iki tarafta aynı kalmalı.
/// Controller'lar IsSuccess'e bakıp Message'ı kullanıcıya gösterir.
/// </summary>
public class BaseResponse<T>
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static BaseResponse<T> Fail(string message) =>
        new() { IsSuccess = false, Message = message };
}
