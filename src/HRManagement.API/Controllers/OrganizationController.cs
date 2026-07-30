using HRManagement.API.Models;
using HRManagement.API.Models.Organization;
using HRManagement.Application.Features.Organization.Queries.GetOrganization;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/organization")]
public class OrganizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Organizasyon şeması: departman → birim → kadro.
    ///
    /// Rol kısıtı YOK ve bu bilinçli bir karardır: kim hangi birimde çalışıyor,
    /// birimin sorumlusu kim — bu şirket içi rehber bilgisidir, girişli herkes
    /// görür. EmployeeVisibility'nin koruduğu PERSONEL DOSYASI ise buradan
    /// sızmaz: yanıt yalnızca ad, kıdem ve kadro bağı taşır; e-posta, telefon,
    /// T.C., doğum tarihi ve izin verisi bu uçtan hiç okunmaz.
    ///
    /// Kişi detayı isteyen istemci /api/employees/{id}/detail'e gider; orada
    /// görünürlük kuralı tam olarak işler.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var o = await _mediator.Send(new GetOrganizationQuery());

        var data = new OrganizationResponse(
            o.Departments.Select(d => new OrgDepartmentResponse(d.Id, d.Name)).ToList(),
            o.Units.Select(u => new OrgUnitResponse(u.Id, u.DepartmentId, u.Name)).ToList(),
            o.Members.Select(m => new OrgMemberResponse(
                m.Id, m.FullName, m.Seniority, m.DepartmentId,
                m.UnitId, m.IsActive, m.IsIntern, m.Grade)).ToList());

        return Ok(BaseResponse<OrganizationResponse>.Success(data));
    }
}
