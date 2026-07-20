using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetInternById;

public sealed record GetInternByIdQuery(int Id) : IRequest<InternDto?>;
