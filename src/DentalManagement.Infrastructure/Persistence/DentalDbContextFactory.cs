using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DentalManagement.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> build the model without starting the API host.
/// </summary>
/// <remarks>
/// Migration authoring only ever needs the model, not a reachable server, so the
/// connection string here is a placeholder unless
/// <c>DENTALMANAGEMENT_CONNECTIONSTRING</c> is set. Real connection strings come
/// from environment configuration at runtime and are never checked in
/// (spec FR-03).
/// </remarks>
public class DentalDbContextFactory : IDesignTimeDbContextFactory<DentalDbContext>
{
    private const string DesignTimePlaceholder =
        "Host=localhost;Database=dentalmanagement_designtime;Username=postgres;Password=postgres";

    public DentalDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DENTALMANAGEMENT_CONNECTIONSTRING")
            ?? DesignTimePlaceholder;

        var options = new DbContextOptionsBuilder<DentalDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new DentalDbContext(options);
    }
}
