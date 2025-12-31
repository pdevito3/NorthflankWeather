using NorthflankWeather.AppHost;
using Serilog;
using Serilog.Events;

// Bootstrap logger to catch startup errors before host is built
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Aspire.Hosting.Dcp", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting NorthflankWeather.AppHost");

    var builder = DistributedApplication.CreateBuilder(args);

    builder.Services.AddSerilog((_, lc) => lc
        .ReadFrom.Configuration(builder.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithProperty("Application", "NorthflankWeather.AppHost"));

    var authProvider = AuthProviders.DuendeDemo();

    var postgres = builder.AddPostgres("postgres")
        .WithPgAdmin();

    var appDb = postgres.AddDatabase("appdb");

    var server = builder.AddProject<Projects.NorthflankWeather_Server>("server")
        .WithReference(appDb)
        .WaitFor(appDb)
        .WithServerAuth(authProvider)
        .WithHttpHealthCheck("/health")
        .WithExternalHttpEndpoints();

    var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
        .WithEndpoint("http", endpoint =>
        {
            endpoint.Port = 5173;
            endpoint.IsProxied = false;
        })
        .WithPnpm();

    var bff = builder.AddProject<Projects.NorthflankWeather_Bff>("bff")
        .WithBffAuth(authProvider)
        .WithReference(server)
        .WithReference(webfrontend)
        .WaitFor(server)
        .WithHttpHealthCheck("/health")
        .WithExternalHttpEndpoints();

    if (authProvider.AuthResource is not null)
    {
        server.WaitFor(authProvider.AuthResource);
        bff.WaitFor(authProvider.AuthResource);
    }

    webfrontend
        .WithReference(bff)
        .WaitFor(bff);

    server.PublishWithContainerFiles(webfrontend, "wwwroot");

    builder.Build().Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "AppHost terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
