using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Organization.Queries.GetOrganization;

/// <summary>
/// Departman + birim + kadro üyeliğini tek yanıtta toplar.
///
/// EmployeeVisibility BİLEREK kullanılmaz: organizasyon şeması şirket içi
/// rehber bilgisidir, personel dosyası değildir. Buna karşılık DTO yalnızca
/// ad/kıdem/kadro bağı taşır — hassas alan hiç okunmaz, dolayısıyla sızacak
/// bir şey de yoktur.
///
/// Pasif çalışanlar da döner (şemada "pasif" olarak işaretlenirler); stajyerler
/// ayrı tablodan gelir ve IsIntern ile işaretlenir.
/// </summary>
public sealed class GetOrganizationQueryHandler : IRequestHandler<GetOrganizationQuery, OrganizationDto>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;

    public GetOrganizationQueryHandler(
        IDepartmentRepository departmentRepository,
        IUnitRepository unitRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository)
    {
        _departmentRepository = departmentRepository;
        _unitRepository = unitRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
    }

    public async Task<OrganizationDto> Handle(GetOrganizationQuery request, CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.GetAllAsync();
        var units = await _unitRepository.GetAllAsync();
        var employees = await _employeeRepository.GetAllAsync();
        var interns = await _internRepository.GetAllAsync();

        var dto = new OrganizationDto
        {
            Departments = departments
                .Select(d => new OrgDepartmentDto { Id = d.Id, Name = d.Name })
                .ToList(),

            Units = units
                .Select(u => new OrgUnitDto { Id = u.Id, DepartmentId = u.DepartmentId, Name = u.Name })
                .ToList()
        };

        dto.Members = employees
            .Select(e => new OrgMemberDto
            {
                Id = e.Id,
                FullName = $"{e.FirstName} {e.LastName}",
                Seniority = e.Seniority is null ? null : (int)e.Seniority,
                DepartmentId = e.DepartmentId,
                UnitId = e.UnitId,
                IsActive = e.IsActive,
                IsIntern = false
            })
            .Concat(interns.Select(i => new OrgMemberDto
            {
                Id = i.Id,
                FullName = $"{i.FirstName} {i.LastName}",
                Seniority = null,
                DepartmentId = i.DepartmentId,
                UnitId = i.UnitId,
                // Stajyer kaydında aktiflik bayrağı yok; staj bitmemişse aktif sayılır.
                IsActive = i.EndDate.Date >= DateTime.Today,
                IsIntern = true,
                Grade = i.Grade
            }))
            .ToList();

        return dto;
    }
}
