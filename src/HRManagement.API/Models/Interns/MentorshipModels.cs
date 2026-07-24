namespace HRManagement.API.Models.Interns;

// Mentorluk uçlarının modelleri. Bu yanıtları YALNIZCA stajyerin mentoru alır
// (MentorshipGuard); Status enum ADI olarak taşınır ("Pending"/"InProgress"/"Done"),
// Türkçe karşılığı WebUI'da verilir.

public sealed class MentoredInternDetailResponse
{
    public MentoredInternDetailResponse(
        int id,
        string firstName,
        string lastName,
        string email,
        string university,
        string major,
        int grade,
        DateTime startDate,
        DateTime endDate,
        string departmentName,
        List<InternTaskResponse> tasks,
        List<InternNoteResponse> notes)
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
        DepartmentName = departmentName;
        Tasks = tasks;
        Notes = notes;
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
    public string DepartmentName { get; }
    public List<InternTaskResponse> Tasks { get; }
    public List<InternNoteResponse> Notes { get; }
}

public sealed class InternTaskResponse
{
    public InternTaskResponse(
        int id, string title, string? description, string status,
        DateTime? dueDate, DateTime createdAt)
    {
        Id = id;
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        CreatedAt = createdAt;
    }

    public int Id { get; }
    public string Title { get; }
    public string? Description { get; }
    public string Status { get; }
    public DateTime? DueDate { get; }
    public DateTime CreatedAt { get; }
}

public sealed class InternNoteResponse
{
    public InternNoteResponse(int id, string authorName, string content, DateTime createdAt)
    {
        Id = id;
        AuthorName = authorName;
        Content = content;
        CreatedAt = createdAt;
    }

    public int Id { get; }
    public string AuthorName { get; }
    public string Content { get; }
    public DateTime CreatedAt { get; }
}

public sealed class AddInternTaskRequest
{
    public AddInternTaskRequest(string title, string? description, DateTime? dueDate)
    {
        Title = title;
        Description = description;
        DueDate = dueDate;
    }

    public string Title { get; }
    public string? Description { get; }
    public DateTime? DueDate { get; }
}

public sealed class AddInternNoteRequest
{
    public AddInternNoteRequest(string content)
    {
        Content = content;
    }

    public string Content { get; }
}

public sealed class UpdateInternTaskStatusRequest
{
    public UpdateInternTaskStatusRequest(int status)
    {
        Status = status;
    }

    /// <summary>InternTaskStatus'un sayısal karşılığı: 1=Pending 2=InProgress 3=Done.</summary>
    public int Status { get; }
}
