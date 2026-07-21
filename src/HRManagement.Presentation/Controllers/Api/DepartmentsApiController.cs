using HRManagement.Application.Features.Departments.Commands.CreateDepartment;
using HRManagement.Application.Features.Departments.Queries.GetAllDepartments;
using HRManagement.Application.Features.Departments.Queries.GetDepartmentById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Presentation.Controllers.Api;

[ApiController]
[Route("api/departments")]
public class DepartmentsApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var departments = await _mediator.Send(new GetAllDepartmentsQuery());
        return Ok(departments);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var department = await _mediator.Send(new GetDepartmentByIdQuery(id));

        if (department is null)
            return NotFound();

        return Ok(department);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateDepartmentCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }
}
