using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.Auth;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// Login çağrısı. Bu istemciye bilinçli olarak BearerTokenHandler EKLENMEZ:
/// giriş yapılırken henüz ortada bir token yoktur.
/// </summary>
public interface IAuthApi
{
    [Post("/api/auth/login")]
    Task<BaseResponse<LoginApiResponse>> LoginAsync([Body] LoginApiRequest request);
}
