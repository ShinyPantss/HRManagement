using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using MediatR;

namespace HRManagement.Application.Features.Users.Commands.DeleteUser;

public sealed class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInternRepository _internRepository;

    public DeleteUserCommandHandler(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IInternRepository internRepository)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _internRepository = internRepository;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id);

        if (user is null)
            throw new ValidationException("Kullanıcı bulunamadı.");

        // Hesap bir çalışan/stajyer kaydına bağlıysa silmek o kaydı sahipsiz bırakır.
        // Erişimi kesmek isteniyorsa doğru yol silmek değil, IsActive = false yapmaktır:
        // LoginCommandHandler pasif hesapları zaten reddediyor.
        if (await _employeeRepository.ExistsByUserIdAsync(request.Id))
            throw new ValidationException(
                "Bu hesap bir çalışan kaydına bağlı. Erişimi kapatmak için hesabı pasife alın.");

        if (await _internRepository.ExistsByUserIdAsync(request.Id))
            throw new ValidationException(
                "Bu hesap bir stajyer kaydına bağlı. Erişimi kapatmak için hesabı pasife alın.");

        await _userRepository.DeleteAsync(request.Id);

        return Unit.Value;
    }
}
