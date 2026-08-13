using DentalManagement.Api.Authorization;
using DentalManagement.Api.DevelopmentOnly;
using DentalManagement.Domain.Abstractions;
using DentalManagement.Infrastructure;
using DentalManagement.Infrastructure.Persistence;
using DentalManagement.Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

// BL-001 exposes no feature endpoint. This host owns DI, environment-based
// configuration, and the migrate/seed entry point; controllers belong to later
// backlog items (spec FR-03, "API Changes: none").

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<DentalDbContext>();

// BL-020: the first feature endpoint. Controllers give automatic
// field-scoped ProblemDetails on model validation (spec FR-12); AddProblemDetails
// extends that RFC 9457 shape to the exception handler and default responses too.
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// CORS for the Vite dev client (design D-8) — the frontend runs on a separate
// origin/port during development and calls this API directly.
const string ViteDevClientCorsPolicy = "ViteDevClient";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ViteDevClientCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// BL-020's ICurrentUser/IPermissionChecker seams (ST-002, ST-003 — spec FR-14,
// FR-15; design D-4, D-5) have no real implementation yet: BL-002 and BL-007
// supply those. The dev-only stubs are wired in only when the host environment
// is Development AND this flag is explicitly set, exactly the shape
// AdminBootstrapOptions.AllowDevelopmentDemoAccounts already uses to keep known
// dev-only behaviour out of production. Every other boot — including the flag
// set outside Development — fails fast at startup rather than serving an
// endpoint with no working authentication/authorization behind it, which would
// otherwise surface later as a per-request runtime failure instead of an
// unambiguous unfinished-dependency message (design D-5).
var developmentAuthOptions = new DevelopmentAuthOptions();
builder.Configuration.GetSection(DevelopmentAuthOptions.ConfigurationSection).Bind(developmentAuthOptions);

if (builder.Environment.IsDevelopment() && developmentAuthOptions.AllowDevelopmentAuthenticationStub)
{
    builder.Services
        .AddAuthentication(StubAuthenticationHandler.SchemeName)
        .AddScheme<AuthenticationSchemeOptions, StubAuthenticationHandler>(
            StubAuthenticationHandler.SchemeName, _ => { });

    builder.Services.AddAuthorization();
    builder.Services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

    builder.Services.AddSingleton<ICurrentUser, StubCurrentUser>();
    builder.Services.AddSingleton<IPermissionChecker, StubPermissionChecker>();
}
else
{
    throw new InvalidOperationException(
        "No authentication/authorization is available for this boot. The dev-only stubs for " +
        "ICurrentUser (ST-002) and IPermissionChecker (ST-003) are registered only when the " +
        "host environment is Development and " +
        $"'{DevelopmentAuthOptions.ConfigurationSection}:" +
        $"{nameof(DevelopmentAuthOptions.AllowDevelopmentAuthenticationStub)}' is explicitly " +
        "set, and the real implementations — BL-002 (authenticated session) and BL-007 " +
        "(server-side authorization) — have not been built yet. " +
        (builder.Environment.IsDevelopment()
            ? "Set the flag in appsettings.Development.json to run locally."
            : $"Refusing to start an unprotected host in the '{builder.Environment.EnvironmentName}' environment."));
}

var app = builder.Build();

// Applying migrations and seeding at startup is opt-in. Doing it unconditionally
// would mean a rolling deployment races itself, and — per the migration runbook —
// seeding must follow a legacy data migration rather than precede it.
if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();

    await scope.ServiceProvider.GetRequiredService<DentalDbContext>()
        .Database.MigrateAsync();

    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<AdminAccountSeeder>().SeedAsync();
}

app.UseCors(ViteDevClientCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
