using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Enums;
using MediatR;

// HRManagement.Domain.Entities BİLİNÇLİ olarak using'lenmedi: oradaki Unit
// (departman birimi) MediatR.Unit ile ad çakışması yapıyor. User tek yerde
// geçtiği için tam nitelikli yazmak alias'tan daha az sürprizli.

namespace HRManagement.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    // Input validation UpdateUserCommandValidator'da.
    // Burada yalnızca veritabanına bakan İŞ KURALLARI kalır.
    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);

        if (user is null)
            throw new ValidationException("Kullanıcı bulunamadı.");

        var email = request.Email.Trim();

        await EnsureEmailIsFree(email, request.Id);
        EnsureNotChangingOwnAccess(request, user);
        await EnsureLastAdminSurvives(request, user);

        user.Email = email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        await _userRepository.UpdateAsync(user);

        return Unit.Value;
    }

    /// <summary>
    /// E-posta BAŞKA bir kullanıcıdaysa reddet. Kaydın kendi e-postasını
    /// koruması serbest olmalı, bu yüzden Id karşılaştırması şart.
    /// </summary>
    private async Task EnsureEmailIsFree(string email, int userId)
    {
        var existingByEmail = await _userRepository.GetByEmailAsync(email);

        if (existingByEmail is not null && existingByEmail.Id != userId)
            throw new ValidationException("Bu e-posta zaten kullanılıyor.");
    }

    /// <summary>
    /// Kimse KENDİ rolünü veya erişimini değiştiremez. İki ayrı kazayı birden kapatır:
    /// Admin'in kendini yanlışlıkla pasife alması ve rolünü düşürüp hesap yönetimine
    /// bir daha girememesi. E-posta değişikliği serbesttir — yetkiye dokunmaz.
    /// Gerçekten gerekliyse işlemi BAŞKA bir Admin yapar; bu da tek kişinin tek
    /// hamlede sistemi kilitlemesini imkânsız kılar.
    /// </summary>
    private static void EnsureNotChangingOwnAccess(
        UpdateUserCommand request, Domain.Entities.User user)
    {
        if (request.Id != request.CurrentUserId)
            return;

        if (request.Role != user.Role)
            throw new ValidationException("Kendi rolünüzü değiştiremezsiniz.");

        if (!request.IsActive)
            throw new ValidationException("Kendi hesabınızı pasife alamazsınız.");
    }

    /// <summary>
    /// Son aktif Admin korunur: rolü düşürülemez, pasife alınamaz. Hesap yönetimine
    /// girebilen tek rol Admin olduğu için sayı sıfıra düşerse sistem uygulama
    /// içinden onarılamaz — veritabanına elle müdahale gerekirdi.
    /// </summary>
    private async Task EnsureLastAdminSurvives(
        UpdateUserCommand request, Domain.Entities.User user)
    {
        // Yalnızca AKTİF bir Admin'i admin'likten çıkaran değişiklikler sayımı düşürür.
        var losesAdmin = user.Role == Role.Admin && user.IsActive
                         && (request.Role != Role.Admin || !request.IsActive);

        if (!losesAdmin)
            return;

        if (await _userRepository.CountActiveAdminsAsync() <= 1)
            throw new ValidationException(
                "Sistemdeki son aktif yönetici hesabı pasife alınamaz veya rolü değiştirilemez.");
    }
}
