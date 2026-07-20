using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Interns.Queries.GetAllInterns;

public sealed class GetAllInternsQueryHandler : IRequestHandler<GetAllInternsQuery, IEnumerable<InternDto>>
{
    private readonly IInternRepository _internRepository;

    public GetAllInternsQueryHandler(IInternRepository internRepository)
    {
        _internRepository = internRepository;
    }

    public async Task<IEnumerable<InternDto>> Handle(GetAllInternsQuery request, CancellationToken cancellationToken)
    {
        var interns = await _internRepository.GetAllAsync();
        return interns.Select(InternMapping.ToDto);
    }
}
