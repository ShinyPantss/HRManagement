namespace HRManagement.API.Models.Interns;

public sealed class CreateInternRequest
{
    public CreateInternRequest(
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

public sealed class UpdateInternRequest
{
    public UpdateInternRequest(
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

public sealed class InternResponse
{
    public InternResponse(
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
