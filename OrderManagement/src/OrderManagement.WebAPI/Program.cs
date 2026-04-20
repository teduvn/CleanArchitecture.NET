using Microsoft.AspNetCore.Authentication.JwtBearer;
using OrderManagement.Application;
using OrderManagement.Domain;
using OrderManagement.Infrastructure;
using OrderManagement.WebAPI.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ── Domain + Application + Infrastructure ────────────────────────────
builder.Services
    .AddDomainServices()                              // Domain Service
    .AddApplicationServices()   // MediatR, Validation, Mapping
    .AddInfrastructureServices(builder.Configuration, builder.Environment); // DB, Cache, External

// ── Presentation-specific ────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Authentication & Authorization
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => { /* ... */ });

// ICurrentUserService — Presentation-specific implementation
// Interface định nghĩa ở Application, implementation ở WebApi
builder.Services.AddHttpContextAccessor();
//builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Host.UseDefaultServiceProvider((context, options) =>
{
    // Validate ngay lúc build, không đợi đến runtime
    options.ValidateScopes =
        context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild =
        context.HostingEnvironment.IsDevelopment();
});


// ── Build & Middleware Pipeline ───────────────────────────────────────
var app = builder.Build();

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
