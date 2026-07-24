using HRManagement.API.Models;
using HRManagement.API.Models.Employees;
using HRManagement.Application.Features.Employees.Commands.CreateEmployee;
using HRManagement.Application.Features.Employees.Commands.DeleteEmployee;
using HRManagement.Application.Features.Employees.Commands.UpdateEmployee;
using HRManagement.Application.Features.Employees.Commands.AddEmployeeNote;
using HRManagement.Application.Features.Employees.Queries.GetAllEmployees;
using HRManagement.Application.Features.Employees.Queries.GetEmployeeById;
using HRManagement.Application.Features.Employees.Queries.GetEmployeeDetail;
using HRManagement.Application.Features.Employees.Queries.GetMyEmployeeDetail;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Liste herkese açıktır ama İÇERİĞİ role göre daralır:
    // Admin/HR hepsini, Manager yalnızca ekibini, Employee yalnızca kendini görür.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _mediator.Send(new GetAllEmployeesQuery(CurrentUserId()));
        var data = employees.Select(ToResponse).ToList();
        return Ok(BaseResponse<List<EmployeeResponse>>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _mediator.Send(new GetEmployeeByIdQuery(id, CurrentUserId()));

        if (e is null)
            return NotFound(BaseResponse<EmployeeResponse>.Fail("Çalışan bulunamadı."));

        return Ok(BaseResponse<EmployeeResponse>.Success(ToResponse(e)));
    }

    // Detay/profil görünümü. Rol attribute'u YOK: görme yetkisi ve alan kırpma
    // (TC yalnız HR, izin açıklaması Manager'a kapalı) Application'da çözülür.
    [HttpGet("{id:int}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var detail = await _mediator.Send(new GetEmployeeDetailQuery(id, CurrentUserId()));

        if (detail is null)
            return NotFound(BaseResponse<EmployeeDetailResponse>.Fail("Çalışan bulunamadı."));

        return Ok(BaseResponse<EmployeeDetailResponse>.Success(ToDetailResponse(detail)));
    }

    // "Profilim": kimlik token'dan çözülür, id parametresi YOK — kişi bu yoldan
    // yalnızca kendi kaydına ulaşabilir.
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var detail = await _mediator.Send(new GetMyEmployeeDetailQuery(CurrentUserId()));

        if (detail is null)
            return NotFound(BaseResponse<EmployeeDetailResponse>.Fail(
                "Hesabınız bir çalışan kaydına bağlı değil."));

        return Ok(BaseResponse<EmployeeDetailResponse>.Success(ToDetailResponse(detail)));
    }

    // Not girme (§5.2): "HR veya Yönetici". Admin bilinçli olarak dışarıda —
    // sistem rolüdür, İK değerlendirmesi yazmaz. Manager'ın "yalnızca kendi
    // ekibine" kısıtı handler'da (rol attribute'u zinciri bilemez).
    [Authorize(Roles = "HR,Manager")]
    [HttpPost("{id:int}/notes")]
    public async Task<IActionResult> AddNote(int id, AddEmployeeNoteRequest request)
    {
        var noteId = await _mediator.Send(new AddEmployeeNoteCommand(id, CurrentUserId(), request.Content));
        return Ok(BaseResponse<int>.Success(noteId, "Not eklendi."));
    }

    // Çalışan kaydı AÇMAK yalnızca İK işidir (kullanıcı kararı, 2026-07-23):
    // Admin sistem rolüdür, personel dosyası açmaz — TC/not kararlarıyla aynı çizgi.
    [Authorize(Roles = "HR")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request)
    {
        var id = await _mediator.Send(new CreateEmployeeCommand(
            request.FirstName, request.LastName, request.NationalId, request.Email,
            request.Phone, request.BirthDate, request.HireDate,
            request.DepartmentId, request.UnitId, request.UserId, request.ManagerId,
            (HRManagement.Domain.Enums.SeniorityLevel?)request.Seniority, request.AnnualLeaveDays,
            CurrentUserId(), request.RequestLoginAccount));
        return CreatedAtAction(nameof(GetById), new { id },
            BaseResponse<int>.Success(id, "Çalışan oluşturuldu."));
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequest request)
    {
        await _mediator.Send(new UpdateEmployeeCommand(
            id, request.FirstName, request.LastName, request.NationalId, request.Email,
            request.Phone, request.BirthDate, request.HireDate,
            request.DepartmentId, request.UnitId, request.UserId, request.ManagerId,
            (HRManagement.Domain.Enums.SeniorityLevel?)request.Seniority, request.AnnualLeaveDays,
            request.IsActive));
        return Ok(BaseResponse<int>.Success(id, "Çalışan güncellendi."));
    }

    // Silme yalnızca Admin (HR ekler/düzenler ama silemez).
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteEmployeeCommand(id));
        return Ok(BaseResponse<int>.Success(id, "Çalışan silindi."));
    }

    /// <summary>Kimlik daima imzalı token'dan okunur, istek gövdesinden asla.</summary>
    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static EmployeeResponse ToResponse(HRManagement.Application.DTOs.EmployeeDto e) => new(
        e.Id, e.FirstName, e.LastName, e.NationalId, e.Email, e.Phone,
        e.BirthDate, e.HireDate, e.DepartmentId, e.UnitId,
        e.UserId, e.ManagerId, e.Seniority, e.AnnualLeaveDays, e.IsActive);

    private static EmployeeDetailResponse ToDetailResponse(HRManagement.Application.DTOs.EmployeeDetailDto d) => new(
        d.Id, d.FirstName, d.LastName, d.NationalId, d.Email, d.Phone,
        d.BirthDate, d.HireDate, d.Seniority, d.IsActive,
        d.DepartmentId, d.DepartmentName, d.ManagerId, d.ManagerFullName,
        d.AccruedLeaveDays, d.UsedLeaveDays, d.RemainingLeaveDays,
        d.RecentLeaveRequests.Select(l => new EmployeeDetailLeaveRequestResponse(
            l.Id, l.Type.ToString(), l.StartDate, l.EndDate,
            l.TotalDays, l.Status.ToString(), l.Description)).ToList(),
        d.DirectReports.Select(t => new EmployeeDetailTeamMemberResponse(
            t.Id, t.FullName, t.Seniority)).ToList(),
        d.MentoredInterns.Select(i => new EmployeeDetailInternResponse(
            i.Id, i.FullName, i.University, i.StartDate, i.EndDate)).ToList(),
        d.Notes?.Select(n => new EmployeeDetailNoteResponse(
            n.Id, n.AuthorName, n.Content, n.CreatedAt)).ToList());
}
