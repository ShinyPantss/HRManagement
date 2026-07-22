using HRManagement.API.Models;
using HRManagement.API.Models.Employees;
using HRManagement.Application.Features.Employees.Commands.CreateEmployee;
using HRManagement.Application.Features.Employees.Commands.DeleteEmployee;
using HRManagement.Application.Features.Employees.Commands.UpdateEmployee;
using HRManagement.Application.Features.Employees.Queries.GetAllEmployees;
using HRManagement.Application.Features.Employees.Queries.GetEmployeeById;
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

    // Çalışan kaydı açmak/değiştirmek/silmek İK işidir; Manager yapamaz.
    [Authorize(Roles = "HR,Admin")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request)
    {
        var id = await _mediator.Send(new CreateEmployeeCommand(
            request.FirstName, request.LastName, request.NationalId, request.Email,
            request.Phone, request.BirthDate, request.HireDate, request.Position,
            request.DepartmentId, request.UserId, request.ManagerId, request.AnnualLeaveDays));
        return CreatedAtAction(nameof(GetById), new { id },
            BaseResponse<int>.Success(id, "Çalışan oluşturuldu."));
    }

    [Authorize(Roles = "HR,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequest request)
    {
        await _mediator.Send(new UpdateEmployeeCommand(
            id, request.FirstName, request.LastName, request.NationalId, request.Email,
            request.Phone, request.BirthDate, request.HireDate, request.Position,
            request.DepartmentId, request.UserId, request.ManagerId, request.AnnualLeaveDays,
            request.IsActive));
        return Ok(BaseResponse<int>.Success(id, "Çalışan güncellendi."));
    }

    [Authorize(Roles = "HR,Admin")]
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
        e.BirthDate, e.HireDate, e.Position, e.DepartmentId,
        e.UserId, e.ManagerId, e.AnnualLeaveDays, e.IsActive);
}
