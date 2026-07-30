using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Users;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// API'nin hesap yönetimi uçlarının sözleşmesi. Uçların tamamı API tarafında
/// Admin'e kilitlidir; buradaki arayüz yalnızca çağrının şeklini tarif eder.
/// </summary>
public interface IUserApi
{
    [Get("/api/users")]
    Task<BaseResponse<List<UserResponse>>> GetAllAsync();

    [Get("/api/users/{id}")]
    Task<BaseResponse<UserResponse>> GetByIdAsync(int id);

    [Put("/api/users/{id}")]
    Task<BaseResponse<int?>> UpdateAsync(int id, [Body] UpdateUserRequest request);
}
