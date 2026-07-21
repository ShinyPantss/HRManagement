using System.ComponentModel.DataAnnotations;
using HRManagement.Application.Features.Departments.Queries.GetAllDepartments;
using HRManagement.Application.Features.Employees.Commands.CreateEmployee;
using HRManagement.Application.Features.Employees.Queries.GetAllEmployees;
using HRManagement.Presentation.Models.Employees;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRManagement.Presentation.Controllers;

public class EmployeesController : Controller
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> Index()
    {
        var employees = await _mediator.Send(new GetAllEmployeesQuery());
        return View(employees);
    }

    public async Task<IActionResult> Create()
    {
        var form = new EmployeeFormViewModel();
        await FillDepartmentOptionsAsync(form);
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel form)
    {
        if (!ModelState.IsValid)
        {
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        var command = new CreateEmployeeCommand(
            FirstName: form.FirstName,
            LastName: form.LastName,
            NationalId: form.NationalId,
            Email: form.Email,
            Phone: form.Phone,
            BirthDate: form.BirthDate!.Value,
            HireDate: form.HireDate!.Value,
            Position: form.Position,
            DepartmentId: form.DepartmentId!.Value);

        try
        {
            await _mediator.Send(command);
        }
        catch (ValidationException ex)
        {
            // Application katmanının son savunma hattı; mesajı forma yansıt.
            ModelState.AddModelError(string.Empty, ex.Message);
            await FillDepartmentOptionsAsync(form);
            return View(form);
        }

        TempData["Success"] = "Çalışan başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillDepartmentOptionsAsync(EmployeeFormViewModel form)
    {
        var departments = await _mediator.Send(new GetAllDepartmentsQuery());
        form.DepartmentOptions = departments
            .Select(d => new SelectListItem(d.Name, d.Id.ToString()));
    }
}
