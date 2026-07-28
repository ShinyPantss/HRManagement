namespace HRManagement.WebUI.Models.Interns;

/// <summary>
/// Mentor dropdown'ının bir adayı. Departman/birim bilgisi option'a data-attribute
/// olarak yazılır; JS, seçilen departman+birime göre süzer (UX — otorite API'de).
/// </summary>
public sealed record MentorOption(int Id, string Name, int DepartmentId, int? UnitId);
