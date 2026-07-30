using HRManagement.WebUI.Models.Api.Users;
using HRManagement.WebUI.Models.Users;
using HRManagement.WebUI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.WebUI.Controllers;

/// <summary>
/// Hesap yönetimi — Admin'in çekirdek ekranı. Rol atama ve erişim kapatma
/// yalnızca buradan yapılır.
///
/// Ekrandaki "kendi hesabını kilitleme" göstergeleri UX içindir; kuralın kendisi
/// (kendi rolünü değiştirememe, son yöneticiyi koruma) Application katmanındadır.
/// Kimliği de WebUI değil API belirler: buradaki eşleştirme kullanıcı adına bakar,
/// API ise imzalı token'daki id'ye.
/// </summary>
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IUserApi _userApi;

    public UsersController(IUserApi userApi)
    {
        _userApi = userApi;
    }

    public async Task<IActionResult> Index()
    {
        var response = await _userApi.GetAllAsync();

        if (!response.IsSuccess)
        {
            TempData["Error"] = response.Message ?? "Hesap listesi alınamadı.";
            return View(new UserListViewModel());
        }

        var users = (response.Data ?? [])
            .Select(u => new UserRowViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                IsSelf = IsSelf(u.Username)
            })
            // Önce roller (Admin üstte), sonra kullanıcı adı: aynı yetki
            // seviyesindeki hesaplar yan yana dursun.
            .OrderBy(u => u.Role).ThenBy(u => u.Username)
            .ToList();

        return View(new UserListViewModel { Users = users });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var response = await _userApi.GetByIdAsync(id);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response.Message ?? "Hesap bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        return View(ToForm(response.Data));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserFormViewModel form)
    {
        if (!ModelState.IsValid)
            return View(form);

        var response = await _userApi.UpdateAsync(form.Id, new UpdateUserRequest
        {
            Email = form.Email,
            Role = form.Role,
            IsActive = form.IsActive
        });

        if (!response.IsSuccess)
        {
            // API'nin iş kuralı reddetti (son yönetici, kendi rolü, e-posta çakışması) —
            // mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, response.Message ?? "İşlem başarısız.");
            return View(form);
        }

        TempData["Success"] = response.Message ?? "Hesap güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Listeden tek tıkla erişimi kapatma/açma. Ayrı bir API ucu değil, aynı
    /// güncelleme ucudur: e-posta ve rol olduğu gibi geri gönderilir.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var current = await _userApi.GetByIdAsync(id);

        if (!current.IsSuccess || current.Data is null)
        {
            TempData["Error"] = current.Message ?? "Hesap bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var response = await _userApi.UpdateAsync(id, new UpdateUserRequest
        {
            Email = current.Data.Email,
            Role = current.Data.Role,
            IsActive = !current.Data.IsActive
        });

        if (!response.IsSuccess)
            TempData["Error"] = response.Message ?? "İşlem başarısız.";
        else
            TempData["Success"] = current.Data.IsActive
                ? "Hesap pasife alındı."
                : "Hesap yeniden etkinleştirildi.";

        return RedirectToAction(nameof(Index));
    }

    private UserFormViewModel ToForm(UserResponse u) => new()
    {
        Id = u.Id,
        Username = u.Username,
        Email = u.Email,
        Role = u.Role,
        IsActive = u.IsActive,
        IsSelf = IsSelf(u.Username)
    };

    // Cookie ticket'ında kullanıcı id'si YOK (yalnızca Name ve Role claim'leri var).
    // Kullanıcı adı benzersiz olduğu için eşleştirme onun üzerinden yapılır.
    private bool IsSelf(string username) =>
        string.Equals(username, User.Identity?.Name, StringComparison.OrdinalIgnoreCase);
}
