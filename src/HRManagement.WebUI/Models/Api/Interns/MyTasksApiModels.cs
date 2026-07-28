using HRManagement.WebUI.Models.Api.Mentorship;

namespace HRManagement.WebUI.Models.Api.Interns;

// API'nin Models/Interns/MyTasksModels tipiyle aynı JSON şekli.
// Görev tipi mentorluk modelleriyle ortak (InternTaskResponse).
public class MyInternTasksResponse
{
    public string? MentorFullName { get; set; }   // null = henüz mentor atanmamış

    public List<InternTaskResponse> Tasks { get; set; } = [];
}
