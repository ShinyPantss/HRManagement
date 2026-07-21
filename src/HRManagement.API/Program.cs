using HRManagement.API.Middleware;
using HRManagement.Application;
using HRManagement.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Composition root: Application (MediatR handler'ları) + Infrastructure (Dapper repo'ları, DB).
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Global hata yönetimi (6.3): ValidationException → 400 ProblemDetails.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
