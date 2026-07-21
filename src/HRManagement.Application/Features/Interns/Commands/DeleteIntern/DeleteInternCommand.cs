using MediatR;

namespace HRManagement.Application.Features.Interns.Commands.DeleteIntern;

public sealed record DeleteInternCommand(int Id) : IRequest<Unit>;
