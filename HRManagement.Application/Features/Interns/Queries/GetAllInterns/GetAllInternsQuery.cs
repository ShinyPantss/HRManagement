using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetAllInterns;

public sealed record GetAllInternsQuery : IRequest<IEnumerable<InternDto>>;
