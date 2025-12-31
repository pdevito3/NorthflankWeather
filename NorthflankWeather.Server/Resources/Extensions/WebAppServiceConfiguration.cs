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

        var connectionString = ConnectionStringHelper.ConvertPostgresUri(
            builder.Configuration.GetConnectionString(DatabaseConsts.DatabaseName));

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
}
