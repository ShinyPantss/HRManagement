using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetInternById;

public sealed class GetInternByIdQuery : IRequest<InternDto?>
{
    public GetInternByIdQuery(int id)
    {
        Id = id;
    }

    public int Id { get; }
}
