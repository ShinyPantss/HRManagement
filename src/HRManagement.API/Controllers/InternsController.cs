using HRManagement.API.Models;
using HRManagement.API.Models.Interns;
using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Interns.Commands.AddInternNote;
using HRManagement.Application.Features.Interns.Commands.AddInternTask;
using HRManagement.Application.Features.Interns.Commands.CreateIntern;
using HRManagement.Application.Features.Interns.Commands.DeleteIntern;
using HRManagement.Application.Features.Interns.Commands.UpdateIntern;
using HRManagement.Application.Features.Interns.Commands.UpdateInternTaskStatus;
using HRManagement.Application.Features.Interns.Queries.GetAllInterns;
using HRManagement.Application.Features.Interns.Queries.GetInternById;
using HRManagement.Application.Features.Interns.Queries.GetMentoredInternDetail;
using HRManagement.Application.Features.Interns.Queries.GetMyMentoredInterns;
using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    // Stajyer LİSTESİNE Manager ve Employee erişemez (kullanıcı kararı) —
    // kendi stajyerlerine /mentored ucundan ulaşırlar. Otorite API'dedir:
    // WebUI'nin menüde gizlemesi tek başına koruma sayılmaz.
    [Authorize(Roles = "HR,Admin,Intern")]
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

    // ── Mentorluk uçları ─────────────────────────────────────────────────────
    // Rol attribute'u YOK: yetki rolden değil İLİŞKİDEN doğar (Interns.MentorId
    // → istekçinin çalışan kaydı). Kuralı MentorshipGuard işletir; Manager da
    // Employee da mentor olabilir. Mentor olmayan boş liste / hata alır.

    // "Mentorluk" listesi: yalnızca kişinin KENDİ stajyerleri.
    [HttpGet("mentored")]
    public async Task<IActionResult> GetMyMentoredInterns()
    {
        var interns = await _mediator.Send(new GetMyMentoredInternsQuery(CurrentUserId()));
        var data = interns.Select(ToResponse).ToList();
        return Ok(BaseResponse<List<InternResponse>>.Success(data));
    }

    // Mentorun stajyer detay sayfası: bilgiler + görevler + notlar tek çağrıda.
    [HttpGet("{id:int}/mentorship")]
    public async Task<IActionResult> GetMentorshipDetail(int id)
    {
        var detail = await _mediator.Send(new GetMentoredInternDetailQuery(id, CurrentUserId()));
        return Ok(BaseResponse<MentoredInternDetailResponse>.Success(ToMentorshipResponse(detail)));
    }

    [HttpPost("{id:int}/tasks")]
    public async Task<IActionResult> AddTask(int id, AddInternTaskRequest request)
    {
        var taskId = await _mediator.Send(new AddInternTaskCommand(
            id, CurrentUserId(), request.Title, request.Description, request.DueDate));
        return Ok(BaseResponse<int>.Success(taskId, "Görev atandı."));
    }

    [HttpPost("{id:int}/notes")]
    public async Task<IActionResult> AddNote(int id, AddInternNoteRequest request)
    {
        var noteId = await _mediator.Send(new AddInternNoteCommand(id, CurrentUserId(), request.Content));
        return Ok(BaseResponse<int>.Success(noteId, "Not eklendi."));
    }

    [HttpPut("tasks/{taskId:int}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int taskId, UpdateInternTaskStatusRequest request)
    {
        await _mediator.Send(new UpdateInternTaskStatusCommand(taskId, CurrentUserId(), request.Status));
        return Ok(BaseResponse<int>.Success(taskId, "Görev durumu güncellendi."));
    }

    // Stajyer kaydı AÇMAK yalnızca İK işidir — çalışan tarafıyla aynı kural.
    [Authorize(Roles = "HR")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateInternRequest request)
    {
        var id = await _mediator.Send(new CreateInternCommand(
            request.FirstName, request.LastName, request.Email, request.University,
            request.Major, request.Grade, request.StartDate, request.EndDate,
            request.MentorId, request.DepartmentId, request.UnitId,
            CurrentUserId(), request.RequestLoginAccount));
        return CreatedAtAction(nameof(GetById), new { id },
            BaseResponse<int>.Success(id, "Stajyer oluşturuldu."));
    }

    // Güncelleme çalışan tarafıyla aynı: HR + Admin. (Bu uçta hiç rol kısıtı
    // yoktu — girişli HERKES stajyer güncelleyebiliyordu; delik kapatıldı.)
    [Authorize(Roles = "HR,Admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateInternRequest request)
    {
        await _mediator.Send(new UpdateInternCommand(
            id, request.FirstName, request.LastName, request.Email, request.University,
            request.Major, request.Grade, request.StartDate, request.EndDate,
            request.MentorId, request.DepartmentId, request.UnitId));
        return Ok(BaseResponse<int>.Success(id, "Stajyer güncellendi."));
    }

    // Silme yalnızca Admin (çalışan silmeyle tutarlı).
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _mediator.Send(new DeleteInternCommand(id));
        return Ok(BaseResponse<int>.Success(id, "Stajyer silindi."));
    }

    private static InternResponse ToResponse(InternDto i) => new(
        i.Id, i.FirstName, i.LastName, i.Email, i.University, i.Major,
        i.Grade, i.StartDate, i.EndDate, i.MentorId, i.DepartmentId, i.UnitId, i.UserId);

    private static MentoredInternDetailResponse ToMentorshipResponse(MentoredInternDetailDto d) => new(
        d.Id, d.FirstName, d.LastName, d.Email, d.University, d.Major,
        d.Grade, d.StartDate, d.EndDate, d.DepartmentName,
        d.Tasks.Select(t => new InternTaskResponse(
            t.Id, t.Title, t.Description,
            ((HRManagement.Domain.Enums.InternTaskStatus)t.Status).ToString(),
            t.DueDate, t.CreatedAt)).ToList(),
        d.Notes.Select(n => new InternNoteResponse(
            n.Id, n.AuthorName, n.Content, n.CreatedAt)).ToList());

    /// <summary>Kimlik daima imzalı token'dan okunur, istek gövdesinden asla.</summary>
    private int CurrentUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
