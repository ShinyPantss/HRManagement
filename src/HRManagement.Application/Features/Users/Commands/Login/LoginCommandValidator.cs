using FluentValidation;

namespace HRManagement.Application.Features.Users.Commands.Login;

/// <summary>
/// Input validation. Handler'a hiç ulaşmadan ValidationBehavior tarafından çalıştırılır.
///
/// DİKKAT — burada mesajlar bilinçli olarak AYNI:
/// Login akışının tamamı (kullanıcı yok / şifre yanlış / hesap pasif / alan boş)
/// TEK TİP "Kullanıcı adı veya şifre hatalı." mesajıyla reddedilir. Boş alan için
/// "Şifre zorunludur." gibi ayrı bir mesaj vermek, cevabın hangi aşamada üretildiğini
/// ele verir ve saldırgana ipucu olur (user enumeration). Bu yüzden hangi alanın boş
/// olduğunu SIZDIRMIYORUZ; iki kural da aynı cümleyi döner.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.UsernameOrEmail)
            .NotEmpty().WithMessage("Kullanıcı adı veya şifre hatalı.");

        RuleFor(command => command.Password)
            .NotEmpty().WithMessage("Kullanıcı adı veya şifre hatalı.");
    }
}
