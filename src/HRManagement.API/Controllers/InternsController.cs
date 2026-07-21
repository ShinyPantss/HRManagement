using HRManagement.API.Models;
using HRManagement.API.Models.Interns;
using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Interns.Commands.CreateIntern;
using HRManagement.Application.Features.Interns.Commands.DeleteIntern;
using HRManagement.Application.Features.Interns.Commands.UpdateIntern;
using HRManagement.Application.Features.Interns.Queries.GetAllInterns;
using HRManagement.Application.Features.Interns.Queries.GetInternById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/interns")]
public class InternsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InternsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var interns = await _mediator.Send(new GetAllInternsQuery());
        var data = interns.Select(ToResponse).ToList();
        return Ok(BaseResponse<List<InternResponse>>.Success(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var i = await _mediator.Send(new GetInternByIdQuery(id));

        if (i is null)
            return NotFound(BaseResponse<InternResponse>.Fail("Stajyer bulunamadı."));

        return Ok(BaseResponse<InternResponse>.Success(ToResponse(i)));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateInternRequest request)
    {
        var id = await _mediator.Send(new CreateInternCommand(
            request.FirstName, request.LastName, request.Email, request.University,
            request.Major, request.Grade, request.StartDate, request.EndDate,
            request.MentorId, request.DepartmentId));
        return CreatedAtAction(nameof(GetById), new { id },
            BaseResponse<int>.Success(id, "Stajyer oluşturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateInternRequest request)
    {
        await _mediator.Send(new UpdateInternCommand(
            id, request.FirstName, request.LastName, request.Email, request.University,
            request.Major, request.Grade, request.StartDate, request.EndDate,
            request.MentorId, request.DepartmentId));
        return Ok(BaseResponse<int>.Success(id, "Stajyer güncellendi."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteInternCommand(id));
        return Ok(BaseResponse<int>.Success(id, "Stajyer silindi."));
    }

    private static InternResponse ToResponse(InternDto i) => new(
        i.Id, i.FirstName, i.LastName, i.Email, i.University, i.Major,
        i.Grade, i.StartDate, i.EndDate, i.MentorId, i.DepartmentId);
}
