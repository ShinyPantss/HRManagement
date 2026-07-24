using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Interfaces;
using HRManagement.Domain.Entities;
using HRManagement.Domain.Enums;
using MediatR;

namespace HRManagement.Application.Features.Employees.Commands.AddEmployeeNote;

public sealed class AddEmployeeNoteCommandHandler : IRequestHandler<AddEmployeeNoteCommand, int>
{
    private readonly IEmployeeNoteRepository _noteRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;

    public AddEmployeeNoteCommandHandler(
        IEmployeeNoteRepository noteRepository,
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository)
    {
        _noteRepository = noteRepository;
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
    }

    // Input validation (içerik boş/uzun) Validator'da; buradaki kurallar DB'ye bakar.
    public async Task<int> Handle(AddEmployeeNoteCommand request, CancellationToken cancellationToken)
    {
        var author = await GetActiveAuthorAsync(request.AuthorUserId);

        var employee = await _employeeRepository.GetByIdAsync(request.EmployeeId);
        if (employee is null)
            throw new ValidationException("Çalışan bulunamadı.");

        await EnsureCanWriteNoteAsync(author, employee);

        return await _noteRepository.AddAsync(new EmployeeNote
        {
            EmployeeId = employee.Id,
            AuthorUserId = author.Id,
            Content = request.Content.Trim()
        });
    }

    private async Task<User> GetActiveAuthorAsync(int userId)
    {
        var author = await _userRepository.GetByIdAsync(userId);

        if (author is null || !author.IsActive)
            throw new ValidationException("İşlemi yapan hesap bulunamadı veya pasif.");

        return author;
    }

    /// <summary>
    /// Kim not girebilir (§5.2 "HR veya Yönetici tarafından girilen"):
    ///   HR      → herkese.
    ///   Manager → yalnızca kendi zincirindeki ekibine; kendi kaydına giremez
    ///             (kendi hakkında not = kendi görmeyeceği veri üretmek olurdu).
    ///   Admin dahil diğer roller → giremez. Admin sistem rolüdür, İK
    ///   değerlendirmesi yazmaz — TC kararıyla aynı çizgi.
    /// </summary>
    private async Task EnsureCanWriteNoteAsync(User author, Employee target)
    {
        if (author.Role == Role.HR)
            return;

        if (author.Role == Role.Manager)
        {
            var authorEmployee = await _employeeRepository.GetByUserIdAsync(author.Id);

            if (authorEmployee is not null
                && authorEmployee.Id != target.Id
                && await _employeeRepository.IsInManagerChainAsync(authorEmployee.Id, target.Id))
                return;
        }

        throw new ValidationException("Bu çalışana not ekleme yetkiniz yok.");
    }
}
