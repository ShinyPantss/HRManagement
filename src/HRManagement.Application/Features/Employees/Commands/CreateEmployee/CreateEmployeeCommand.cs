using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.CreateEmployee;

/// <summary>
/// "Yeni çalışan ekle" isteği. Geriye yeni kaydın Id'si döner.
///
/// Position (serbest metin) YOK: pozisyon Departman + Seniority'den türetilir.
/// UserId    → giriş hesabıyla ilişkilendirme (5.2; sonradan da bağlanabilir)
/// ManagerId → bağlı olduğu yönetici; izin onay zinciri buradan kurulur
/// Seniority → kıdem/ünvan seviyesi (GM, GMY, Müdür...)
/// AnnualLeaveDays → izin hakkını elle ezme; normalde null (kıdemden hesaplanır)
/// CreatedByUserId → kaydı açan (imzalı JWT'den); otomatik hesap talebinin
///   "talep eden"i olur, denetim izi doğru kalsın diye gövdeden değil claim'den gelir.
/// RequestLoginAccount → true ise çalışan eklenince Admin'e otomatik hesap talebi
///   düşer (HR ayrı adım atmasın). Zaten bir hesaba bağlandıysa (UserId dolu) etkisiz.
/// </summary>
public sealed record CreateEmployeeCommand(
    string FirstName,
    string LastName,
    string? NationalId,
    string Email,
    string? Phone,
    DateTime BirthDate,
    DateTime HireDate,
    int DepartmentId,
    int? UnitId,
    int? UserId,
    int? ManagerId,
    SeniorityLevel? Seniority,
    int? AnnualLeaveDays,
    int CreatedByUserId,
    bool RequestLoginAccount) : IRequest<int>;
