using HRManagement.WebUI.Models.Api;
using HRManagement.WebUI.Models.Api.LeaveRequests;
using Refit;

namespace HRManagement.WebUI.Services;

/// <summary>
/// API'nin izin talebi uçlarının sözleşmesi. Refit implementasyonu çalışma anında üretir.
/// DİKKAT: "tüm talepleri getir" ucu yoktur — liste her zaman bir çalışana göre gelir.
/// </summary>
public interface ILeaveRequestApi
{
    [Get("/api/leaverequests/employee/{employeeId}")]
    Task<BaseResponse<List<LeaveRequestResponse>>> GetByEmployeeAsync(int employeeId);

    // Giriş yapanın onayını bekleyen talepler (çalışan seçmeye gerek yok).
    [Get("/api/leaverequests/pending-approvals")]
    Task<BaseResponse<List<PendingApprovalResponse>>> GetPendingApprovalsAsync();

    [Post("/api/leaverequests")]
    Task<BaseResponse<int?>> CreateAsync([Body] CreateLeaveRequestRequest request);

    [Post("/api/leaverequests/{id}/approve")]
    Task<BaseResponse<int?>> ApproveAsync(int id);

    [Post("/api/leaverequests/{id}/reject")]
    Task<BaseResponse<int?>> RejectAsync(int id, [Body] RejectLeaveRequestRequest request);

    [Delete("/api/leaverequests/{id}")]
    Task<BaseResponse<int?>> DeleteAsync(int id);
}
