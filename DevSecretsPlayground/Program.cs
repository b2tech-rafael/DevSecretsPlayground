using Microsoft.Extensions.Configuration;
using DotNetEnv;

static string? GetWinningProvider(IConfigurationRoot root, string key)
{
    // Configuration providers are evaluated in order; the last provider wins.
    // Iterate from last to first to find who supplied the final value.
    foreach (var provider in root.Providers.Reverse())
    {
        if (provider.TryGet(key, out _))
        {
            return provider.GetType().Name;
        }
    }

    return null;
}

var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

// Load .env into environment variables (only if present).
// This simulates the common Docker Compose / local dev workflow where .env becomes env vars.
Env.Load(".env");

Console.WriteLine("CWD: " + Directory.GetCurrentDirectory());
Console.WriteLine("appsettings.json exists: " + File.Exists("appsettings.json"));
Console.WriteLine($"appsettings.{env}.json exists: " + File.Exists($"appsettings.{env}.json"));
Console.WriteLine(".env exists: " + File.Exists(".env"));
Console.WriteLine("ApiIntegration__ApiKey env: " + (Environment.GetEnvironmentVariable("ApiIntegration__ApiKey") ?? "<null>"));
Console.WriteLine();

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    // Lowest
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    //// Lowest
    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
    // .NET User Secrets (dev only; works if you init + set them)
    .AddUserSecrets<Program>(optional: true)
    // Environment variables (includes values set by .env and launchSettings.json)
    // .AddEnvironmentVariables()
    // Highest
    .AddCommandLine(args);

var configRoot = builder.Build();

var keys = new[]
{
    "Db:ConnectionString",
    "ApiIntegration:ApiKey"
};

Console.WriteLine("=== Config Hierarchy Live Demo ===");
Console.WriteLine($"ASPNETCORE_ENVIRONMENT: {env}");
Console.WriteLine();
Console.WriteLine("Provider order (low -> high), last wins:");
Console.WriteLine("  1) appsettings.json");
Console.WriteLine("  2) secrets.json (optional)");
Console.WriteLine("  3) .NET User Secrets (Development/local)");
Console.WriteLine("  4) Environment Variables (includes .env + launchSettings.json)");
Console.WriteLine("  5) Command-line arguments");
Console.WriteLine();

foreach (var k in keys)
{
    var val = configRoot[k];
    var winner = GetWinningProvider(configRoot, k) ?? "<none>";
    Console.WriteLine($"{k} = {val}");
    Console.WriteLine($"  winner: {winner}");
}
