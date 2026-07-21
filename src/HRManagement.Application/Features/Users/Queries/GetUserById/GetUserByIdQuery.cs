using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQuery : IRequest<UserDto?>
{
    public GetUserByIdQuery(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
