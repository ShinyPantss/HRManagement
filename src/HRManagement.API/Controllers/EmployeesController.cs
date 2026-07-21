using HRManagement.API.Models;
using HRManagement.API.Models.Employees;
using HRManagement.Application.Features.Employees.Commands.CreateEmployee;
using HRManagement.Application.Features.Employees.Commands.DeleteEmployee;
using HRManagement.Application.Features.Employees.Commands.UpdateEmployee;
using HRManagement.Application.Features.Employees.Queries.GetAllEmployees;
using HRManagement.Application.Features.Employees.Queries.GetEmployeeById;
using MediatR;
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

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _mediator.Send(new GetAllEmployeesQuery());
        var data = employees.Select(ToResponse).ToList();
        return Ok(BaseResponse<List<EmployeeResponse>>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var e = await _mediator.Send(new GetEmployeeByIdQuery(id));

        if (e is null)
            return NotFound(BaseResponse<EmployeeResponse>.Fail("Çalışan bulunamadı."));

        return Ok(BaseResponse<EmployeeResponse>.Success(ToResponse(e)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateEmployeeRequest request)
    {
        var id = await _mediator.Send(new CreateEmployeeCommand(
            request.FirstName, request.LastName, request.NationalId, request.Email,
            request.Phone, request.BirthDate, request.HireDate, request.Position, request.DepartmentId));
        return CreatedAtAction(nameof(GetById), new { id },
            BaseResponse<int>.Success(id, "Çalışan oluşturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateEmployeeRequest request)
    {
        await _mediator.Send(new UpdateEmployeeCommand(
            id, request.FirstName, request.LastName, request.NationalId, request.Email,
            request.Phone, request.BirthDate, request.HireDate, request.Position,
            request.DepartmentId, request.IsActive));
        return Ok(BaseResponse<int>.Success(id, "Çalışan güncellendi."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteEmployeeCommand(id));
        return Ok(BaseResponse<int>.Success(id, "Çalışan silindi."));
    }

    private static EmployeeResponse ToResponse(HRManagement.Application.DTOs.EmployeeDto e) => new(
        e.Id, e.FirstName, e.LastName, e.NationalId, e.Email, e.Phone,
        e.BirthDate, e.HireDate, e.Position, e.DepartmentId, e.IsActive);
}
