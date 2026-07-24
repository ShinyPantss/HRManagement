using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.AddEmployeeNote;

/// <summary>
/// Çalışana not ekler (§5.2). AuthorUserId imzalı token'dan gelir (API
/// CurrentUserId) — kim yazdıysa o kaydedilir, istek gövdesinden yazar alınmaz.
/// </summary>
public sealed record AddEmployeeNoteCommand(int EmployeeId, int AuthorUserId, string Content)
    : IRequest<int>;
