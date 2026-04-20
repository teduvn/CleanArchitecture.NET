using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application;
using OrderManagement.Application.Contracts;
using OrderManagement.Domain;
using OrderManagement.Infrastructure;
using OrderManagement.WebAPI.Extensions;
using OrderManagement.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Domain + Application + Infrastructure ────────────────────────────
builder.Services
    .AddDomainServices()                              // Domain Service
    .AddApplicationServices()   // MediatR, Validation, Mapping
    .AddInfrastructureServices(builder.Configuration, builder.Environment); // DB, Cache, External

// ── Presentation-specific ────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Configure JSON options if needed
    });

// Configure Problem Details
builder.Services.AddProblemDetails();

// Configure API behavior options to properly format Problem Details
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = false;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// ICurrentUserService — Presentation-specific implementation
// Interface định nghĩa ở Application, implementation ở WebApi
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    // Validate ngay lúc build, không đợi đến runtime
    options.ValidateScopes =
        context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild =
        context.HostingEnvironment.IsDevelopment();
});

// Authentication & Authorization
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization(builder.Configuration);

// ── Build & Middleware Pipeline ───────────────────────────────────────
var app = builder.Build();

// CRITICAL: ExceptionMiddleware PHẢI là middleware đầu tiên trong pipeline
// Nếu đặt sau, các middleware trước nó có thể throw exception mà không bị catch
app.UseGlobalExceptionHandling();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Gọi migration và seeding trước khi app lắng nghe request
    await app.InitialiseDatabaseAsync();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();


app.Run();
