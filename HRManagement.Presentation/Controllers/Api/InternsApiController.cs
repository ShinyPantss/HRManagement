using HRManagement.Application.Features.Interns.Commands.CreateIntern;
using HRManagement.Application.Features.Interns.Queries.GetAllInterns;
using HRManagement.Application.Features.Interns.Queries.GetInternById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Presentation.Controllers.Api;

[ApiController]
[Route("api/interns")]
public class InternsApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternsApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var interns = await _mediator.Send(new GetAllInternsQuery());
        return Ok(interns);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var intern = await _mediator.Send(new GetInternByIdQuery(id));

        if (intern is null)
            return NotFound();

        return Ok(intern);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInternCommand command)
    {
        var id = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id }, null);
    }
}
