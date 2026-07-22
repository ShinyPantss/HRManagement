using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.WebUI.Models.AccountRequests;

/// <summary>
/// HR'ın hesap talebi oluşturma formu. Kişi tek bir dropdown'dan seçilir:
/// değer "e:{id}" (çalışan) veya "i:{id}" (stajyer) olarak kodlanır — böylece
/// tek alanla iki tabloyu ayırt ederiz. Yalnızca HESABI OLMAYAN kişiler listelenir.
/// </summary>
public class CreateAccountRequestViewModel
{
    [Required(ErrorMessage = "Kişi seçimi zorunludur.")]
    [Display(Name = "Kişi (hesabı olmayan çalışan/stajyer)")]
    public string? Subject { get; set; }   // "e:12" | "i:3"

    [Required(ErrorMessage = "Önerilen rol zorunludur.")]
    [Display(Name = "Önerilen Rol")]
    public int SuggestedRole { get; set; }

    [MaxLength(500, ErrorMessage = "Not en fazla 500 karakter olabilir.")]
    [Display(Name = "Not (opsiyonel)")]
    public string? Note { get; set; }

    public IEnumerable<SelectListItem> SubjectOptions { get; set; } = [];
    public IEnumerable<SelectListItem> RoleOptions { get; set; } = [];
}
