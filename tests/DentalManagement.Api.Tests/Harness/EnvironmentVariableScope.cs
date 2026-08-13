namespace DentalManagement.Api.Tests.Harness;

/// <summary>
/// Temporarily sets process environment variables, restoring their previous
/// values (or absence) on <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// <c>Program.cs</c> reads its environment (<c>IHostEnvironment.IsDevelopment()</c>)
/// and configuration (<c>AddInfrastructure</c>'s required connection string)
/// before <c>builder.Build()</c> is ever called.
/// <c>WebApplicationFactory.WithWebHostBuilder</c>'s own
/// <c>UseEnvironment</c>/<c>ConfigureAppConfiguration</c> overrides are only
/// applied once the deferred host builder's <c>Build()</c> runs — too late for
/// those pre-<c>Build</c> reads, confirmed by running an actual boot attempt
/// with only those overrides set (it still failed on the missing connection
/// string, not the auth gate). Real process environment variables — exactly
/// the mechanism <c>README.md</c>'s Configuration table documents
/// (<c>ConnectionStrings__DentalManagement</c>) — are read at
/// <c>WebApplication.CreateBuilder</c> time and are visible to every
/// subsequent line, which is what this scope relies on. See
/// <see cref="ApiFactory"/> for where it is used.
/// </remarks>
internal sealed class EnvironmentVariableScope : IDisposable
{
    private readonly Dictionary<string, string?> _previousValues = new();

    public EnvironmentVariableScope(IReadOnlyDictionary<string, string?> variables)
    {
        foreach (var (key, value) in variables)
        {
            _previousValues[key] = Environment.GetEnvironmentVariable(key);
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    public void Dispose()
    {
        foreach (var (key, previousValue) in _previousValues)
        {
            Environment.SetEnvironmentVariable(key, previousValue);
        }
    }
}
