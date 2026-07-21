using System.ComponentModel.DataAnnotations;
using HRManagement.Application.dto.Auth;
using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    // Input validation (boş girdi) LoginCommandValidator'da — ve orada da mesaj
    // bilinçli olarak buradakiyle AYNI: "Kullanıcı adı veya şifre hatalı."
    // Burada yalnızca veritabanına bakan İŞ KURALI kalır.
    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // CreateUser kullanıcı adını ve e-postayı Trim() ederek kaydediyor;
        // arama da aynı şekilde normalize edilmezse baştaki/sondaki boşluk
        // yüzünden geçerli kullanıcı bulunamaz.
        var usernameOrEmail = request.UsernameOrEmail.Trim();

        var user = await _userRepository.GetByUsernameAsync(usernameOrEmail);

        if (user is null)
            user = await _userRepository.GetByEmailAsync(usernameOrEmail);

        bool passwordCorrect = false;

        if (user is not null)
        {
            try
            {
                passwordCorrect = _passwordHasher.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                // Kayıttaki hash boş veya bozuk olabilir (elle eklenmiş satır,
                // eski düz metin parola). BCrypt böyle bir hash'te exception
                // fırlatır; yakalamazsak istek 500 ile döner ve bu, "bu kullanıcı
                // var" bilgisini sızdıran bir fark yaratır. Doğrulama başarısız
                // sayılır ve aşağıdaki tek tip mesaj döner.
                passwordCorrect = false;
            }
        }

        // Kullanıcı yok / şifre yanlış / hesap pasif: ÜÇÜ DE aynı mesajla reddedilir.
        // Farklı mesaj vermek, şifreyi bilmeyen birine hesabın var olduğunu
        // (ve pasif olduğunu) sızdırır — user enumeration açığı.
        // IsActive kontrolü şifre doğrulamasından SONRA değerlendirilir.
        if (user is null || !passwordCorrect || !user.IsActive)
        {
            throw new ValidationException("Kullanıcı adı veya şifre hatalı.");
        }

        string token = _jwtTokenGenerator.Generate(user);

        return new AuthResultDto
        {
            Token = token,
            Username = user.Username,
            Role = user.Role.ToString()
        };
    }
}
