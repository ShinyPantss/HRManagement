using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetInternById;

public sealed class GetInternByIdQueryHandler : IRequestHandler<GetInternByIdQuery, InternDto?>
{
    private readonly IInternRepository _internRepository;

    public GetInternByIdQueryHandler(IInternRepository internRepository)
    {
        _internRepository = internRepository;
    }

    public async Task<InternDto?> Handle(GetInternByIdQuery request, CancellationToken cancellationToken)
    {
        var intern = await _internRepository.GetByIdAsync(request.Id);
        return intern is null ? null : InternMapping.ToDto(intern);
    }
}
