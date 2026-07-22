using System.Security.Claims;
using HRManagement.API.Models;
using HRManagement.API.Models.AccountRequests;
using HRManagement.Application.Features.AccountRequests.Commands.ApproveAccountRequest;
using HRManagement.Application.Features.AccountRequests.Commands.CreateAccountRequest;
using HRManagement.Application.Features.AccountRequests.Commands.RejectAccountRequest;
using HRManagement.Application.Features.AccountRequests.Queries.GetPendingAccountRequests;
using HRManagement.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/accountrequests")]
public class AccountRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Bekleyen talepler — Admin işler.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending()
    {
        var requests = await _mediator.Send(new GetPendingAccountRequestsQuery());
        var data = requests.Select(r => new AccountRequestResponse(
            r.Id, r.EmployeeId, r.InternId, r.SubjectName, r.SubjectType,
            r.RequestedByUserId, r.RequestedByUsername, r.SuggestedRole,
            r.Note, r.Status, r.CreatedAt)).ToList();
        return Ok(BaseResponse<List<AccountRequestResponse>>.Success(data));
    }

    /// <summary>Talep oluştur — yalnızca HR (görev ayrımı; Admin onaylar).</summary>
    [Authorize(Roles = "HR")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequestRequest request)
    {
        var id = await _mediator.Send(new CreateAccountRequestCommand(
            CurrentUserId(), request.EmployeeId, request.InternId,
            (Role)request.SuggestedRole, request.Note));
        return Ok(BaseResponse<int>.Success(id, "Hesap talebi oluşturuldu."));
    }

    /// <summary>Onayla (hesap açılır) — yalnızca Admin.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, ApproveAccountRequestRequest request)
    {
        var userId = await _mediator.Send(new ApproveAccountRequestCommand(
            id, CurrentUserId(), request.Username, request.Email, request.Password,
            request.Role.HasValue ? (Role)request.Role.Value : null));
        return Ok(BaseResponse<int>.Success(userId, "Talep onaylandı, hesap açıldı."));
    }

    /// <summary>Reddet — yalnızca Admin.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, RejectAccountRequestRequest request)
    {
        await _mediator.Send(new RejectAccountRequestCommand(id, CurrentUserId(), request.Reason));
        return Ok(BaseResponse<int>.Success(id, "Talep reddedildi."));
    }

    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
