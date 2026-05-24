using Asp.Versioning;
using GK.GlobalKineticAssessment.API.Configuration;
using GK.GlobalKineticAssessment.API.Middleware;
using GK.GlobalKineticAssessment.Application;
using GK.GlobalKineticAssessment.Infrastructure;
using GK.GlobalKineticAssessment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, _, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .Enrich.FromLogContext());

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.Configure<BasicAuthOptions>(builder.Configuration.GetSection("BasicAuth"));

    builder.Services
        .AddApiVersioning(o =>
        {
            o.DefaultApiVersion = new ApiVersion(1, 0);
            o.AssumeDefaultVersionWhenUnspecified = true;
            o.ReportApiVersions = true;
        })
        .AddApiExplorer(o =>
        {
            o.GroupNameFormat = "'v'VVV";
            o.SubstituteApiVersionInUrl = true;
        });

    builder.Services
        .AddControllers()
        .AddJsonOptions(o =>
            o.JsonSerializerOptions.PropertyNamingPolicy =
                System.Text.Json.JsonNamingPolicy.CamelCase);

    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen(c =>
    {
        c.EnableAnnotations();
        foreach (var xml in Directory.EnumerateFiles(AppContext.BaseDirectory, "GK.*.xml"))
            c.IncludeXmlComments(xml, includeControllerXmlComments: true);
    });

    builder.Services.AddCors(o =>
        o.AddDefaultPolicy(p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("sqlserver");

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                Log.Information("Applying migrations...");
                await db.Database.MigrateAsync();
                Log.Information("Migrations applied successfully.");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Migration step failed — application will continue.");
        }
    }

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<BasicAuthMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseCors();

    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        foreach (var d in app.DescribeApiVersions())
            o.SwaggerEndpoint(
                $"/swagger/{d.GroupName}/swagger.json",
                $"Global Kinetic Assessment API {d.GroupName.ToUpper()}");
        o.RoutePrefix = "swagger";
        o.DisplayRequestDuration();
    });

    if (app.Environment.IsDevelopment())
        app.UseHttpsRedirection();

    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("GK Assessment API started | {Env} | /swagger",
        app.Environment.EnvironmentName);

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Console.WriteLine($"Host terminated unexpectedly: {ex.Message}");
    throw;
}

public partial class Program { }