using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.CreateIntern;

/// <summary>
/// "Yeni stajyer ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
///
/// CreatedByUserId → kaydı açan (imzalı JWT'den); otomatik hesap talebinin
///   "talep eden"i olur (denetim izi). RequestLoginAccount → true ise stajyer
///   eklenince Admin'e otomatik hesap talebi düşer (HR ayrı adım atmasın).
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
    int DepartmentId,
    int? UnitId,
    int CreatedByUserId,
    bool RequestLoginAccount) : IRequest<int>;
