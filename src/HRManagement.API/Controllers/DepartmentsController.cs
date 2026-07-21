using HRManagement.API.Models;
using HRManagement.API.Models.Departments;
using HRManagement.Application.Features.Departments.Commands.CreateDepartment;
using HRManagement.Application.Features.Departments.Commands.DeleteDepartment;
using HRManagement.Application.Features.Departments.Commands.UpdateDepartment;
using HRManagement.Application.Features.Departments.Queries.GetAllDepartments;
using HRManagement.Application.Features.Departments.Queries.GetDepartmentById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _mediator.Send(new GetAllDepartmentsQuery());
        var data = departments
            .Select(d => new DepartmentResponse(d.Id, d.Name, d.Description))
            .ToList();
        return Ok(BaseResponse<List<DepartmentResponse>>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var d = await _mediator.Send(new GetDepartmentByIdQuery(id));

        if (d is null)
            return NotFound(BaseResponse<DepartmentResponse>.Fail("Departman bulunamadı."));

        var data = new DepartmentResponse(d.Id, d.Name, d.Description);
        return Ok(BaseResponse<DepartmentResponse>.Success(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDepartmentRequest request)
    {
        var id = await _mediator.Send(new CreateDepartmentCommand(request.Name, request.Description));
        return CreatedAtAction(nameof(GetById), new { id },
            BaseResponse<int>.Success(id, "Departman oluşturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateDepartmentRequest request)
    {
        await _mediator.Send(new UpdateDepartmentCommand(id, request.Name, request.Description));
        return Ok(BaseResponse<int>.Success(id, "Departman güncellendi."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteDepartmentCommand(id));
        return Ok(BaseResponse<int>.Success(id, "Departman silindi."));
    }
}
