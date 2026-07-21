using System.ComponentModel.DataAnnotations;

namespace HRManagement.WebUI.Models.Departments;

/// <summary>
/// Departman ekleme/düzenleme formunun ekran modeli. API modelinden ayrıdır:
/// burada görüntü validasyonu ve etiketler yaşar.
/// </summary>
public class DepartmentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Departman adı zorunludur.")]
    [Display(Name = "Departman Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Açıklama (opsiyonel)")]
    public string? Description { get; set; }
}
