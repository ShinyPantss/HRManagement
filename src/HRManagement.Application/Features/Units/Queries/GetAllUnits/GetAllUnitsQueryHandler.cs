using HRManagement.Application.DTOs;
using HRManagement.Application.Interfaces;
using HRManagement.Application.Mapping;
using MediatR;

namespace HRManagement.Application.Features.Units.Queries.GetAllUnits;

public sealed class GetAllUnitsQueryHandler : IRequestHandler<GetAllUnitsQuery, IReadOnlyList<UnitDto>>
{
    private readonly IUnitRepository _unitRepository;

    public GetAllUnitsQueryHandler(IUnitRepository unitRepository)
    {
        _unitRepository = unitRepository;
    }

    public async Task<IReadOnlyList<UnitDto>> Handle(GetAllUnitsQuery request, CancellationToken cancellationToken)
    {
        var units = await _unitRepository.GetAllAsync();
        return units.Select(UnitMapping.ToDto).ToList();
    }
}
