using Api.Constants;
using Api.Data;
using Api.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    Log.Information("Starting The App API");
    var builder = WebApplication.CreateBuilder(args);
    var allowedOrigins =
        builder.Configuration.GetSection(AppConfig.AllowedOrigins).Get<string[]>()
        ?? Array.Empty<string>();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            name: AppConfig.CorsPolicy,
            policy =>
            {
                policy
                    .WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            }
        );
    });

    builder.Services.AddDbContext<AppDbContext>(options =>
    {
        var cs = builder.Configuration.GetConnectionString(AppConfig.Postgres);
        options.UseNpgsql(cs);

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    builder.Services.AddSingleton<TextEmbeddingService>();
    builder.Services.AddScoped<SimilarityService>();
    builder.Services.AddScoped<TrendService>();

    builder.Logging.ClearProviders();

    builder.Host.UseSerilog(
        (context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithProcessId()
                .Enrich.WithThreadId();
        }
    );

    var logsPath = Path.Combine(builder.Environment.ContentRootPath, "logs");
    Directory.CreateDirectory(logsPath);

    builder.Services.AddControllers();

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }

    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (System.Exception)
{
    Log.Fatal("The App API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
