using HRManagement.WebUI.Models.Api.LeaveRequests;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.LeaveRequests;

/// <summary>
/// İzin talepleri liste ekranının modeli. API'de "tüm talepler" ucu olmadığı için
/// liste her zaman bir çalışana bağlıdır: ekran hem çalışan seçiciyi hem de
/// seçilen çalışanın taleplerini taşır.
/// </summary>
public class LeaveRequestListViewModel
{
    /// <summary>Seçili çalışan; null ise henüz seçim yapılmamıştır ve liste boş gösterilir.</summary>
    public int? SelectedEmployeeId { get; set; }

    public IEnumerable<SelectListItem> EmployeeOptions { get; set; } = [];

    public List<LeaveRequestResponse> Requests { get; set; } = [];
}
