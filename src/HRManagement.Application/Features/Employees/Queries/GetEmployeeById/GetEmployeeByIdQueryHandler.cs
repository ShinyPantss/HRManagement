using HRManagement.Application.DTOs;
using HRManagement.Application.Features.Employees.Shared;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQueryHandler : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto?>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUserRepository _userRepository;
    private readonly EmployeeVisibility _visibility;

    public GetEmployeeByIdQueryHandler(
        IEmployeeRepository employeeRepository,
        IUserRepository userRepository,
        EmployeeVisibility visibility)
    {
        _employeeRepository = employeeRepository;
        _userRepository = userRepository;
        _visibility = visibility;
    }

    public async Task<EmployeeDto?> Handle(GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        // Yetki kaydı OKUMADAN önce denetlenir; yetkisizse kaydın varlığı bile sızmaz.
        await _visibility.EnsureCanViewAsync(request.RequesterUserId, request.Id);

        var employee = await _employeeRepository.GetByIdAsync(request.Id);

        if (employee is null)
            return null;

        // Hassas alan kırpması detay yoluyla AYNI kaynaktan gelir. Rol DB'den
        // okunur, JWT claim'inden değil — claim bayatlayabilir.
        var requester = await _userRepository.GetByIdAsync(request.RequesterUserId);

        return EmployeeMapping.ToDto(employee, EmployeeFieldVisibility.CanSeeNationalId(requester));
    }
}
