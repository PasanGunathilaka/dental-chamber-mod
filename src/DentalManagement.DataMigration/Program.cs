using DentalManagement.DataMigration;
using DentalManagement.DataMigration.LegacyReaders;
using DentalManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// One-time legacy SQL Server -> PostgreSQL migration (spec FR-20..FR-23).
//
// Connection strings come from the command line or the environment, never from a
// checked-in file (spec FR-03).

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine(Usage);
    return 0;
}

var source = ArgumentValue("--source")
    ?? Environment.GetEnvironmentVariable("DENTALMANAGEMENT_LEGACY_CONNECTIONSTRING");

var targetConnectionString = ArgumentValue("--target")
    ?? Environment.GetEnvironmentVariable("DENTALMANAGEMENT_CONNECTIONSTRING");

if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(targetConnectionString))
{
    Console.Error.WriteLine(
        "Both a source and a target connection string are required." + Environment.NewLine + Usage);
    return 1;
}

var options = new MigrationOptions
{
    SourceConnectionString = source,
    TargetConnectionString = targetConnectionString,
    DryRun = args.Contains("--dry-run"),
    AllowNonEmptyTarget = args.Contains("--allow-non-empty"),
    ReportDirectory = ArgumentValue("--report-dir"),
};

var contextOptions = new DbContextOptionsBuilder<DentalDbContext>()
    .UseNpgsql(options.TargetConnectionString)
    .Options;

await using var target = new DentalDbContext(contextOptions);

var runner = new MigrationRunner(
    new SqlServerLegacyDataSource(options.SourceConnectionString),
    target,
    options);

var outcome = await runner.RunAsync();

Console.WriteLine(outcome.Message);

if (options.ReportDirectory is not null)
{
    Directory.CreateDirectory(options.ReportDirectory);

    if (outcome.Audit is not null)
    {
        await File.WriteAllTextAsync(
            Path.Combine(options.ReportDirectory, "audit-report.json"),
            outcome.Audit.ToJson());
    }

    if (outcome.Reconciliation is not null)
    {
        await File.WriteAllTextAsync(
            Path.Combine(options.ReportDirectory, "reconciliation-report.json"),
            outcome.Reconciliation.ToJson());
    }

    Console.WriteLine($"Reports written to {Path.GetFullPath(options.ReportDirectory)}");
}

return outcome.ExitCode;

string? ArgumentValue(string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}

partial class Program
{
    private const string Usage = """
        Usage: DentalManagement.DataMigration --source <sql-server-cs> --target <postgres-cs> [options]

          --source <cs>        Legacy SQL Server connection string.
                               Falls back to DENTALMANAGEMENT_LEGACY_CONNECTIONSTRING.
          --target <cs>        Target PostgreSQL connection string.
                               Falls back to DENTALMANAGEMENT_CONNECTIONSTRING.
          --dry-run            Read, audit, and report without writing anything.
          --allow-non-empty    Permit a target that already holds domain data.
                               Refused by default: a bulk one-way migration onto a
                               populated target risks a half-merged database.
          --report-dir <path>  Write audit-report.json and reconciliation-report.json here.

        Exit code is 0 only when the migration ran and every reconciliation check agreed.
        """;
}
