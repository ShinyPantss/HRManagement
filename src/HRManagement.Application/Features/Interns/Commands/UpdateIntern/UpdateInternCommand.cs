using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.UpdateIntern;

public sealed class UpdateInternCommand : IRequest<Unit>
{
    public UpdateInternCommand(
        int id,
        string firstName,
        string lastName,
        string email,
        string university,
        string major,
        int grade,
        DateTime startDate,
        DateTime endDate,
        int? mentorId,
        int departmentId)
    {
        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        University = university;
        Major = major;
        Grade = grade;
        StartDate = startDate;
        EndDate = endDate;
        MentorId = mentorId;
        DepartmentId = departmentId;
    }

    public int Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string University { get; }
    public string Major { get; }
    public int Grade { get; }
    public DateTime StartDate { get; }
    public DateTime EndDate { get; }
    public int? MentorId { get; }
    public int DepartmentId { get; }
}
