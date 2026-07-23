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
    int? UserId,
    int? ManagerId,
    SeniorityLevel? Seniority,
    int? AnnualLeaveDays) : IRequest<int>;
