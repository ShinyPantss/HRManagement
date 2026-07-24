using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;

namespace HRManagement.Application.Features.Units.Shared;

/// <summary>
/// "Seçilen birim, kişinin departmanına ait mi?" kuralı — Create/Update ve
/// Employee/Intern handler'larının ortak kalbi. Birim OPSİYONEL: null ise atlanır.
/// Doluysa birim var olmalı ve DepartmentId'si kişinin departmanıyla eşleşmeli
/// (yoksa "Malın birimi Oto çalışanına" gibi tutarsız bağlar oluşurdu).
///
/// Not: Domain.Unit tipini bilerek adıyla YAZMIYORUZ (var ile çözülür) —
/// MediatR.Unit ile karışmasın; bu yüzden helper'ı çağıran handler'lar
/// Domain.Entities'i import etmek zorunda kalmaz.
/// </summary>
public static class UnitAssignment
{
    public static async Task EnsureUnitInDepartmentAsync(
        IUnitRepository unitRepository, int? unitId, int departmentId)
    {
        if (unitId is not int id) return;

        var unit = await unitRepository.GetByIdAsync(id);
        if (unit is null)
            throw new ValidationException("Seçilen birim bulunamadı.");
        if (unit.DepartmentId != departmentId)
            throw new ValidationException("Seçilen birim, seçilen departmana ait değil.");
    }
}
