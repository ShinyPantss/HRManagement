using HRManagement.API.Models;
using HRManagement.API.Models.LeaveRequests;
using HRManagement.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Commands.CreateLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Commands.DeleteLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;
using HRManagement.Application.Features.LeaveRequests.Queries.GetLeaveRequestsByEmployee;
using HRManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/leaverequests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaveRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("employee/{employeeId:int}")]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var requests = await _mediator.Send(new GetLeaveRequestsByEmployeeQuery(employeeId));
        var data = requests.Select(r => new LeaveRequestResponse(
            r.Id, r.EmployeeId, r.InternId, r.Type.ToString(), r.StartDate, r.EndDate,
            r.TotalDays, r.Status.ToString(), r.Description, r.RejectionReason, r.CreatedAt)).ToList();
        return Ok(BaseResponse<List<LeaveRequestResponse>>.Success(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateLeaveRequestRequest request)
    {
        var id = await _mediator.Send(new CreateLeaveRequestCommand(
            request.EmployeeId, (LeaveType)request.Type, request.StartDate,
            request.EndDate, request.Description));
        return Ok(BaseResponse<int>.Success(id, "İzin talebi oluşturuldu."));
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        await _mediator.Send(new ApproveLeaveRequestCommand(id));
        return Ok(BaseResponse<int>.Success(id, "İzin talebi onaylandı."));
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectLeaveRequestRequest request)
    {
        await _mediator.Send(new RejectLeaveRequestCommand(id, request.Reason));
        return Ok(BaseResponse<int>.Success(id, "İzin talebi reddedildi."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteLeaveRequestCommand(id));
        return Ok(BaseResponse<int>.Success(id, "İzin talebi silindi."));
    }
}
