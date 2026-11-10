using System.Text.Json.Serialization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Enrichers;
using Nomadica.API.Middleware;
using Nomadica.API.Services;
using Nomadica.Application.Interfaces;
using Nomadica.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfig) =>
{
    var minLevel = context.HostingEnvironment.IsDevelopment()
        ? Serilog.Events.LogEventLevel.Debug
        : Serilog.Events.LogEventLevel.Information;

    loggerConfig
        .MinimumLevel.Is(minLevel)
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithThreadId()
        .Enrich.WithEnvironmentUserName()
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
        .WriteTo.File("logs/nomadica-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);

    // Optional Seq sink — only when configured (e.g. Seq:ServerUrl in Development). A missing or
    // stopped Seq never breaks logging; pointing at Sentry/another platform is likewise config-only.
    var seqServerUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqServerUrl))
        loggerConfig.WriteTo.Seq(seqServerUrl);
});

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("anonymous", opts =>
    {
        opts.PermitLimit = 100;
        opts.Window = TimeSpan.FromMinutes(1);
        opts.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("authenticated", opts =>
    {
        opts.PermitLimit = 300;
        opts.Window = TimeSpan.FromMinutes(1);
        opts.QueueLimit = 0;
    });

    options.AddFixedWindowLimiter("auth-endpoints", opts =>
    {
        opts.PermitLimit = 10;
        opts.Window = TimeSpan.FromMinutes(1);
        opts.QueueLimit = 0;
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.HttpContext.Response.ContentType = "application/problem+json";

        var problem = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Rate limit exceeded.",
            Type = "https://tools.ietf.org/html/rfc6585#section-4",
            Detail = "Too many requests. Please try again later."
        };

        context.HttpContext.Response.Headers.RetryAfter = "60";
        await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
    };
});

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
        options.AddPolicy("DevelopmentCors", policy =>
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
}

var app = builder.Build();

// Run migrations and seed demo data
using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Nomadica.Infrastructure.Persistence.NomadiaDbContext>();
    await dbContext.Database.MigrateAsync();
    await Nomadica.Infrastructure.Persistence.DemoDataSeeder.SeedDemoDataAsync(dbContext);
}

// Console import commands (run instead of the web host), e.g.:
//   dotnet run --project Nomadica.API -- import-osm --iso GR --country Greece [--by-region]
//   dotnet run --project Nomadica.API -- import-csv --file ./places.csv --approve
if (args.Length > 0 && (args[0] == "import-osm" || args[0] == "import-csv"))
{
    static string? ArgValue(string[] all, string key)
    {
        var index = Array.IndexOf(all, key);
        return index >= 0 && index + 1 < all.Length ? all[index + 1] : null;
    }

    using var importScope = app.Services.CreateAsyncScope();
    var services = importScope.ServiceProvider;

    if (args[0] == "import-osm")
    {
        var iso = ArgValue(args, "--iso") ?? "GR";
        var country = ArgValue(args, "--country");
        var autoApprove = !args.Contains("--pending");
        var osm = services.GetRequiredService<Nomadica.Infrastructure.Imports.OsmImportService>();
        // --by-region splits big countries (GR, ES, IN, ID...) into one Overpass query per region
        var report = args.Contains("--by-region")
            ? await osm.ImportCountryByRegionAsync(iso, country, autoApprove)
            : await osm.ImportCountryAsync(iso, country, autoApprove);
        Console.WriteLine($"OSM import [{iso}] -> {report}");
    }
    else
    {
        var file = ArgValue(args, "--file");
        if (string.IsNullOrWhiteSpace(file))
        {
            Console.WriteLine("Provide --file <path.csv>");
            return;
        }
        var autoApprove = args.Contains("--approve");
        var csv = services.GetRequiredService<Nomadica.Infrastructure.Imports.CsvImportService>();
        var report = await csv.ImportFileAsync(file, autoApprove);
        Console.WriteLine($"CSV import [{file}] -> {report}");
    }

    return;
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging(opts =>
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("DevelopmentCors");
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();
