namespace HRManagement.API.Models.Interns;

// UnitId → departmanın alt kırılımı (Birim); opsiyonel, departmana ait olmalı.
// RequestLoginAccount → true ise stajyer eklenince Admin'e otomatik hesap talebi düşer.
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
        int departmentId,
        int? unitId,
        bool requestLoginAccount)
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
        UnitId = unitId;
        RequestLoginAccount = requestLoginAccount;
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
    public int? UnitId { get; }
    public bool RequestLoginAccount { get; }
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
        int departmentId,
        int? unitId)
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
        UnitId = unitId;
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
    public int? UnitId { get; }
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
        int departmentId,
        int? unitId,
        int? userId)
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
        UnitId = unitId;
        UserId = userId;
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
    public int? UnitId { get; }
    public int? UserId { get; }
}
