using System.ComponentModel.DataAnnotations;
using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Interns.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMyInternProfile;

public sealed class GetMyInternProfileQueryHandler
    : IRequestHandler<GetMyInternProfileQuery, MyInternProfileDto>
{
    // Ekranda listeyi boğmamak için son N talep (çalışan profiliyle aynı yaklaşım).
    private const int RecentLeaveRequestCount = 10;

    private readonly IInternRepository _internRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitRepository _unitRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternTaskRepository _taskRepository;
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly UnitManagerResolver _unitManagerResolver;

    public GetMyInternProfileQueryHandler(
        IInternRepository internRepository,
        IDepartmentRepository departmentRepository,
        IUnitRepository unitRepository,
        IEmployeeRepository employeeRepository,
        IInternTaskRepository taskRepository,
        ILeaveRequestRepository leaveRequestRepository,
        UnitManagerResolver unitManagerResolver)
    {
        _internRepository = internRepository;
        _departmentRepository = departmentRepository;
        _unitRepository = unitRepository;
        _employeeRepository = employeeRepository;
        _taskRepository = taskRepository;
        _leaveRequestRepository = leaveRequestRepository;
        _unitManagerResolver = unitManagerResolver;
    }

    public async Task<MyInternProfileDto> Handle(GetMyInternProfileQuery request, CancellationToken cancellationToken)
    {
        var intern = await _internRepository.GetByUserIdAsync(request.RequesterUserId);

        if (intern is null)
            throw new ValidationException("Hesabınız bir stajyer kaydına bağlı değil.");

        var department = await _departmentRepository.GetByIdAsync(intern.DepartmentId);

        var unit = intern.UnitId is int unitId
            ? await _unitRepository.GetByIdAsync(unitId)
            : null;

        var mentor = intern.MentorId is int mentorId
            ? await _employeeRepository.GetByIdAsync(mentorId)
            : null;

        // Yönetici mentor'dan AYRI: birim/departman hiyerarşisinden türetilir.
        var manager = await _unitManagerResolver.ResolveAsync(intern.DepartmentId, intern.UnitId);

        var tasks = (await _taskRepository.GetByInternIdAsync(intern.Id)).ToList();
        var leaveRequests = await _leaveRequestRepository.GetByInternIdAsync(intern.Id);

        return new MyInternProfileDto
        {
            FirstName = intern.FirstName,
            LastName = intern.LastName,
            Email = intern.Email,
            University = intern.University,
            Major = intern.Major,
            Grade = intern.Grade,
            StartDate = intern.StartDate,
            EndDate = intern.EndDate,
            DepartmentName = department?.Name ?? string.Empty,
            UnitName = unit?.Name,
            MentorFullName = mentor is null ? null : $"{mentor.FirstName} {mentor.LastName}",
            ManagerFullName = manager is null ? null : $"{manager.FirstName} {manager.LastName}",
            TotalTasks = tasks.Count,
            DoneTasks = tasks.Count(t => t.Status == InternTaskStatus.Done),
            RecentLeaveRequests = leaveRequests
                .OrderByDescending(l => l.StartDate)
                .Take(RecentLeaveRequestCount)
                .Select(l => new MyInternLeaveRequestDto
                {
                    Id = l.Id,
                    Type = l.Type,
                    StartDate = l.StartDate,
                    EndDate = l.EndDate,
                    TotalDays = l.WorkingDays,
                    Status = l.Status,
                    Description = l.Description   // kişinin kendi talebi
                })
                .ToList()
        };
    }
}
