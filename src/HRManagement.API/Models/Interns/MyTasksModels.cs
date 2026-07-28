namespace HRManagement.API.Models.Interns;

// "Görevlerim" yanıtı (stajyerin kendisi). Görev tipi mentorluk uçlarıyla
// ortak (InternTaskResponse) — iki ekran aynı görevi aynı şekilde gösterir.
public sealed class MyInternTasksResponse
{
    public MyInternTasksResponse(string? mentorFullName, List<InternTaskResponse> tasks)
    {
        MentorFullName = mentorFullName;
        Tasks = tasks;
    }

    /// <summary>null = henüz mentor atanmamış.</summary>
    public string? MentorFullName { get; }

    public List<InternTaskResponse> Tasks { get; }
}
