namespace NorthflankWeather.Server.Resources.Extensions;

using Databases;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public static class WebAppServiceConfiguration
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddProblemDetails();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddApplicationServices();
        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
        builder.Services.AddApiVersioningExtension();
        builder.Services.AddControllers();
        builder.Services.AddSwaggerExtension(builder.Configuration);
        builder.Services.AddJwtBearerAuthentication(builder.Configuration, builder.Environment);

        var connectionString = builder.Configuration.GetConnectionString(DatabaseConsts.DatabaseName);
        connectionString = ConvertPostgresUri(connectionString);

        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        var dataSource = dataSourceBuilder.Build();

        builder.Services.AddSingleton(dataSource);
        builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
        {
            var ds = serviceProvider.GetRequiredService<NpgsqlDataSource>();
            options.UseNpgsql(ds)
                .UseSnakeCaseNamingConvention();
        });
        builder.EnrichNpgsqlDbContext<AppDbContext>();
    }

    /// <summary>
    /// Converts PostgreSQL URI to Npgsql connection string.
    /// Handles Northflank's malformed URI with ?sslmode (no value).
    /// </summary>
    private static string? ConvertPostgresUri(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return connectionString;

        if (!connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
            return connectionString;

        // Remove malformed query string (e.g., ?sslmode with no value)
        var queryIndex = connectionString.IndexOf('?');
        if (queryIndex > 0)
            connectionString = connectionString[..queryIndex];

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':');
        var username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');

        return new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.Require
        }.ConnectionString;
    }
}
