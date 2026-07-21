using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.CreateIntern;

/// <summary>
/// "Yeni stajyer ekle" isteği. IRequest&lt;int&gt;: bu mesaj işlendiğinde
/// geriye yeni kaydın Id'si döner. MediatR bu tipe bakarak handler'ı bulur.
/// </summary>
public sealed class CreateInternCommand : IRequest<int>
{
    public CreateInternCommand(
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
