using FireSystemEventMonitor.Api.Domain;
using FireSystemEventMonitor.Api.Infrastructure;
using FireSystemEventMonitor.Api.Security;
using FireSystemEventMonitor.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IIncidentService, IncidentService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddDbContext<FireMonitorDbContext>(options =>
{
    var provider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
    var connectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
    }
    else
    {
        options.UseSqlite(connectionString);
    }
});

var origins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
{
    if (origins.Length > 0)
    {
        policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod();
    }
}));

var app = builder.Build();

app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var problem = new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "The request could not be completed.",
        Detail = app.Environment.IsDevelopment() ? exception?.Message : null
    };
    context.Response.StatusCode = problem.Status.Value;
    await context.Response.WriteAsJsonAsync(problem);
}));
app.UseCors();
app.UseMiddleware<TenantResolutionMiddleware>();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.MapGet("/api/incidents", async (IIncidentService service, CancellationToken cancellationToken) =>
    Results.Ok(await service.ListAsync(cancellationToken)));

app.MapPost("/api/events", async (
    CreateFireEventRequest request,
    IIncidentService service,
    CancellationToken cancellationToken) =>
{
    var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
    if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            validateAllProperties: true))
    {
        return Results.ValidationProblem(validationResults
            .GroupBy(x => x.MemberNames.FirstOrDefault() ?? string.Empty)
            .ToDictionary(x => x.Key, x => x.Select(y => y.ErrorMessage ?? "Invalid value.").ToArray()));
    }

    var result = await service.RecordEventAsync(request, cancellationToken);
    return Results.Created($"/api/events/{result.EventId}", result);
});

app.MapPost("/api/incidents/{id:guid}/acknowledge", async (
    Guid id,
    IIncidentService service,
    CancellationToken cancellationToken) =>
{
    var incident = await service.AcknowledgeAsync(id, cancellationToken);
    return incident is null ? Results.NotFound() : Results.Ok(incident);
});

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<FireMonitorDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
}

app.Run();

public partial class Program;

