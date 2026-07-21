using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Employees.Queries.GetEmployeeById;

public sealed class GetEmployeeByIdQuery : IRequest<EmployeeDto?>
{
    public GetEmployeeByIdQuery(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
