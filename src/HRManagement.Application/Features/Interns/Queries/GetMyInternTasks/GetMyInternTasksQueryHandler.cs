using System.ComponentModel.DataAnnotations;
using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetMyInternTasks;

public sealed class GetMyInternTasksQueryHandler
    : IRequestHandler<GetMyInternTasksQuery, MyInternTasksDto>
{
    private readonly IInternRepository _internRepository;
    private readonly IInternTaskRepository _taskRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public GetMyInternTasksQueryHandler(
        IInternRepository internRepository,
        IInternTaskRepository taskRepository,
        IEmployeeRepository employeeRepository)
    {
        _internRepository = internRepository;
        _taskRepository = taskRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<MyInternTasksDto> Handle(GetMyInternTasksQuery request, CancellationToken cancellationToken)
    {
        var intern = await _internRepository.GetByUserIdAsync(request.RequesterUserId);

        if (intern is null)
            throw new ValidationException("Hesabınız bir stajyer kaydına bağlı değil.");

        var tasks = await _taskRepository.GetByInternIdAsync(intern.Id);

        var mentor = intern.MentorId is int mentorId
            ? await _employeeRepository.GetByIdAsync(mentorId)
            : null;

        return new MyInternTasksDto
        {
            MentorFullName = mentor is null ? null : $"{mentor.FirstName} {mentor.LastName}",
            Tasks = tasks
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new InternTaskDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Status = (int)t.Status,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt
                })
                .ToList()
        };
    }
}
