namespace HRManagement.WebUI.Models.Api.Mentorship;

// API'nin Models/Interns/MentorshipModels tipleriyle aynı JSON şekli.
// (Paylaşılan Contracts projesi yok — senkron tutmak bizim sorumluluğumuz.)
// Task Status enum ADI gelir ("Pending"/"InProgress"/"Done"); Türkçesi view'da.

public class MentoredInternDetailResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public int Grade { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string DepartmentName { get; set; } = string.Empty;

    public List<InternTaskResponse> Tasks { get; set; } = [];
    public List<InternNoteResponse> Notes { get; set; } = [];
}

public class InternTaskResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class InternNoteResponse
{
    public int Id { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AddInternTaskRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime? DueDate { get; set; }
}

public class AddInternNoteRequest
{
    public string Content { get; set; } = string.Empty;
}

public class UpdateInternTaskStatusRequest
{
    // InternTaskStatus'un sayısal karşılığı: 1=Pending 2=InProgress 3=Done.
    public int Status { get; set; }
}
