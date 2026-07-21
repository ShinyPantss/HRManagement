using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(int Id) : IRequest<UserDto?>;
