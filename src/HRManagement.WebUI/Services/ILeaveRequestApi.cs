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

    // Tek talebin detayı + onay izi; görüntüleme yetkisini API/handler çözer.
    [Get("/api/leaverequests/{id}/detail")]
    Task<BaseResponse<LeaveDetailResponse>> GetDetailAsync(int id);

    // Giriş yapanın onayını bekleyen talepler (çalışan seçmeye gerek yok).
    [Get("/api/leaverequests/pending-approvals")]
    Task<BaseResponse<List<PendingApprovalResponse>>> GetPendingApprovalsAsync();

    // TÜM izin geçmişi (her durumda) — "İzin Geçmişi" ekranı; yalnızca HR/Admin (API rol kapısı).
    [Get("/api/leaverequests/all")]
    Task<BaseResponse<List<LeaveHistoryResponse>>> GetAllAsync();

    [Post("/api/leaverequests")]
    Task<BaseResponse<int?>> CreateAsync([Body] CreateLeaveRequestRequest request);

    [Post("/api/leaverequests/{id}/approve")]
    Task<BaseResponse<int?>> ApproveAsync(int id);

    [Post("/api/leaverequests/{id}/reject")]
    Task<BaseResponse<int?>> RejectAsync(int id, [Body] RejectLeaveRequestRequest request);

    [Delete("/api/leaverequests/{id}")]
    Task<BaseResponse<int?>> DeleteAsync(int id);
}
