using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Units.Queries.GetAllUnits;

/// <summary>
/// Tüm birimleri getirir (departman-birim dropdown'ını doldurmak için).
/// Küçük bir referans listesi olduğu için departmana göre filtreleme istemcide
/// (JS) yapılır; ayrı bir "departmana göre getir" ucu şimdilik gerekmez.
/// </summary>
public sealed record GetAllUnitsQuery() : IRequest<IReadOnlyList<UnitDto>>;
