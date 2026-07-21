using HRManagement.Application;
using HRManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// MVC: UI controller'ları (View döner) + API controller'ları (JSON döner) aynı kayıtla gelir.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

// Geleneksel route UI için (/Employees → EmployeesController.Index);
// API controller'ları kendi [Route("api/...")] attribute'larıyla eşleşir.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
