using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.AccountRequests;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// Hesap talebi uçlarının Refit sözleşmesi. Yetki API tarafında rollerle
/// kısıtlıdır (talep: HR/Admin, onay/red/liste: Admin); WebUI menüde ayrıca gizler.
/// </summary>
public interface IAccountRequestApi
{
    [Get("/api/accountrequests/pending")]
    Task<BaseResponse<List<AccountRequestResponse>>> GetPendingAsync();

    [Post("/api/accountrequests")]
    Task<BaseResponse<int>> CreateAsync([Body] CreateAccountRequestRequest request);

    [Post("/api/accountrequests/{id}/approve")]
    Task<BaseResponse<int>> ApproveAsync(int id, [Body] ApproveAccountRequestRequest request);

    [Post("/api/accountrequests/{id}/reject")]
    Task<BaseResponse<int>> RejectAsync(int id, [Body] RejectAccountRequestRequest request);


}
