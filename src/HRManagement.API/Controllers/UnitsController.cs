using HRManagement.API.Models;
using HRManagement.API.Models.Units;
using HRManagement.Application.Features.Units.Queries.GetAllUnits;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRManagement.API.Controllers;

[ApiController]
[Route("api/units")]
public class UnitsController : ControllerBase
{
    private readonly IMediator _mediator;

    public UnitsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // Departman gibi referans veri: rol kısıtı yok, girişli herkes okuyabilir
    // (form dropdown'ını doldurmak için). Departmana göre süzme istemcide yapılır.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var units = await _mediator.Send(new GetAllUnitsQuery());
        var data = units.Select(u => new UnitResponse(u.Id, u.DepartmentId, u.Name)).ToList();
        return Ok(BaseResponse<List<UnitResponse>>.Success(data));
    }
}
