using Microsoft.Extensions.Configuration;
using DotNetEnv;
using Amazon;

/// <summary>
/// Configuration providers are evaluated in order; the last provider wins.
/// Iterate from last to first to find who supplied the final value.
/// </summary>
static string? GetWinningProvider(IConfigurationRoot root, string key)
{
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

// Toggle SSM provider on/off
var useSsm = string.Equals(
    Environment.GetEnvironmentVariable("AWS_SSM_ENABLED"),
    "true",
    StringComparison.OrdinalIgnoreCase);

// Let region be controlled from env; default to eu-central-1 for demo
var awsRegion = Environment.GetEnvironmentVariable("AWS_REGION") ?? "eu-central-1";
var ssmPath = $"{Environment.GetEnvironmentVariable("AWS_SSM_PATH")}/{Environment.GetEnvironmentVariable("AWS_SSM_SERVICE_PATH")}";

// Load .env into environment variables (only if present).
// This simulates the common Docker Compose / local dev workflow where .env becomes env vars.
Env.Load(".env");

Console.WriteLine("CWD: " + Directory.GetCurrentDirectory());
Console.WriteLine("appsettings.json exists: " + File.Exists("appsettings.json"));
Console.WriteLine($"appsettings.{env}.json exists: " + File.Exists($"appsettings.{env}.json"));
Console.WriteLine(".env exists: " + File.Exists(".env"));
Console.WriteLine("AWS_SSM_ENABLED: " + (Environment.GetEnvironmentVariable("AWS_SSM_ENABLED") ?? "<null>"));
Console.WriteLine("AWS_REGION: " + awsRegion);
Console.WriteLine("ssmPath: " + ssmPath);
Console.WriteLine("ApiIntegration__ApiKey env: " + (Environment.GetEnvironmentVariable("ApiIntegration__ApiKey") ?? "<null>"));
Console.WriteLine();

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    // Lowest
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    //// Lowest
    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false);

if (useSsm)
{
    // AWS Parameter Store (SSM) — we put this ABOVE env vars so env vars can override it.
    builder.AddSystemsManager(configureSource =>
    {
        configureSource.Path = ssmPath;
        configureSource.Optional = false;

        configureSource.AwsOptions = new Amazon.Extensions.NETCore.Setup.AWSOptions
        {
            Region = RegionEndpoint.GetBySystemName(awsRegion)
        };
    });
}

builder
    // .NET User Secrets (dev only; works if you init + set them)
    .AddUserSecrets<Program>(optional: true)
    // Environment variables (includes values set by .env and launchSettings.json)
    .AddEnvironmentVariables()
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

if (useSsm)
{
    Console.WriteLine($"  3) AWS SSM Parameter Store ({ssmPath})");
    Console.WriteLine("  4) .NET User Secrets (Development/local)");
    Console.WriteLine("  5) Environment Variables (includes .env + launchSettings.json)");
    Console.WriteLine("  6) Command-line arguments");
}
else
{
    Console.WriteLine("  3) .NET User Secrets (Development/local)");
    Console.WriteLine("  4) Environment Variables (includes .env + launchSettings.json)");
    Console.WriteLine("  5) Command-line arguments");
}

Console.WriteLine();

foreach (var k in keys)
{
    var val = configRoot[k];
    var winner = GetWinningProvider(configRoot, k) ?? "<none>";
    Console.WriteLine($"{k} = {val}");
    Console.WriteLine($"  winner: {winner}");
}