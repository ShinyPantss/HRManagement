using HRManagement.Application.DTOs;
using MediatR;

namespace HRManagement.Application.Features.Organization.Queries.GetOrganization;

/// <summary>
/// Organizasyon şemasını döndürür. İstekçi parametresi YOKTUR ve bu bilinçlidir:
/// çıktı herkese aynıdır, role göre daralmaz (bkz. OrganizationDto).
/// </summary>
public sealed record GetOrganizationQuery : IRequest<OrganizationDto>;
