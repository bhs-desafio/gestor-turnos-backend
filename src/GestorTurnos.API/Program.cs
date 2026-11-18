using GestorTurnos.API.Endpoints;
using GestorTurnos.API.Common;
using GestorTurnos.Application;
using GestorTurnos.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Gestor de Turnos API";
        document.Info.Version = "v1";
        document.Info.Description = "API para la gestión de colaboradores y turnos.";
        return Task.CompletedTask;
    });
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

var app = builder.Build();

// Crea la base de datos si no existe (o aplica migraciones pendientes).
await app.Services.InitializeDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();                  // /openapi/v1.json
    app.MapScalarApiReference(options => // /scalar
    {
        options.Title = "Gestor de Turnos API";
    });
}

app.UseExceptionHandler();
app.UseCors();

app.UseHttpsRedirection();

app.MapColaboradorEndpoints();

app.Run();
