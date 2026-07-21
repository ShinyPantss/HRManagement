using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Users.Queries.GetAllUsers;

public sealed class GetAllUsersQuery : IRequest<IEnumerable<UserDto>> { }
