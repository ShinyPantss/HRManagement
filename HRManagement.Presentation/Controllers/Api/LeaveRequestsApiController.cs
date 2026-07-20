using HRManagement.Application.DTOs;
using HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestsByEmployee;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.Presentation.Controllers.Api;

[ApiController]
[Route("api/leaverequests")]
public class LeaveRequestsApiController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaveRequestsApiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var requests = await _mediator.Send(new GetLeaveRequestsByEmployeeQuery(employeeId));
        return Ok(requests);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveRequestCommand command)
    {
        var id = await _mediator.Send(command);
        return Ok(new { id });
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _mediator.Send(new ApproveLeaveRequestCommand(id));
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectDto dto)
    {
        await _mediator.Send(new RejectLeaveRequestCommand(id, dto.Reason));
        return NoContent();
    }
}
