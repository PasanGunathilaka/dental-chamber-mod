using DentalManagement.Infrastructure;
using DentalManagement.Infrastructure.Persistence;
using DentalManagement.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

// BL-001 exposes no feature endpoint. This host owns DI, environment-based
// configuration, and the migrate/seed entry point; controllers belong to later
// backlog items (spec FR-03, "API Changes: none").

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks().AddDbContextCheck<DentalDbContext>();

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

app.MapHealthChecks("/health");

app.Run();
