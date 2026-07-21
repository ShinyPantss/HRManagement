using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.CreateIntern;

/// <summary>
/// "Yeni stajyer ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
/// </summary>
public sealed record CreateInternCommand(
    string FirstName,
    string LastName,
    string Email,
    string University,
    string Major,
    int Grade,
    DateTime StartDate,
    DateTime EndDate,
    int? MentorId,
    int DepartmentId) : IRequest<int>;
