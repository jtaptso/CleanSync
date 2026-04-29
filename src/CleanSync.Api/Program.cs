using CleanSync.Application.DTOs;
using CleanSync.Application.Interfaces;
using CleanSync.Application.Services;
using CleanSync.Api.HealthChecks;
using CleanSync.Domain.Interfaces;
using CleanSync.Infrastructure.Data;
using CleanSync.Infrastructure.Repositories;
using CleanSync.Infrastructure.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Configure Swagger with enhanced settings
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "CleanSync API",
        Version = "v1",
        Description = "API for synchronizing Business Partners between SAP and E-commerce platforms",
        Contact = new() { Name = "CleanSync Support", Email = "support@cleansync.io" },
        License = new() { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });
    
    // Use full type name for schema IDs to avoid conflicts
    c.CustomSchemaIds(type => type.FullName);
});

// Configure structured logging
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";
    options.UseUtcTimestamp = true;
});
builder.Logging.AddDebug();

// Configure SQL Server or In-Memory based on configuration
var useInMemory = builder.Configuration.GetValue<bool>("UseInMemoryDb");
var demoMode = builder.Configuration.GetValue<bool>("DemoMode");

if (useInMemory)
{
    builder.Services.AddDbContext<CleanSyncDbContext>(options =>
        options.UseInMemoryDatabase("CleanSyncDb"));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<CleanSyncDbContext>(options =>
        options.UseSqlServer(connectionString));
}

// Register application services
builder.Services.AddScoped<IBusinessPartnerRepository, BusinessPartnerRepository>();
builder.Services.AddScoped<ISyncLogRepository, SyncLogRepository>();

if (demoMode)
{
    builder.Services.AddScoped<ISapBusinessPartnerService, MockSapBusinessPartnerService>();
    builder.Services.AddScoped<IEcommerceBusinessPartnerService, MockEcommerceBusinessPartnerService>();
}
else
{
    builder.Services.AddScoped<ISapBusinessPartnerService, SapServiceLayerBusinessPartnerService>();
    builder.Services.AddScoped<IEcommerceBusinessPartnerService, MockEcommerceBusinessPartnerService>();
}

builder.Services.AddScoped<BusinessPartnerSyncService>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CleanSyncDbContext>("database")
    .AddCheck<SapConnectionHealthCheck>("sap")
    .AddCheck<EcommerceConnectionHealthCheck>("ecommerce");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Enable Swagger UI with enhanced settings
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CleanSync API v1");
        c.DocumentTitle = "CleanSync API Documentation";
        
        // Enhanced UI features
        c.DefaultModelsExpandDepth(-1); // Collapse models by default
        c.DisplayRequestDuration();     // Show request duration
        c.EnableDeepLinking();          // Enable deep linking for URLs
        c.EnableTryItOutByDefault();   // Enable Try It Out by default
        c.ShowExtensions();            // Show OpenAPI extensions
    });
}

// Request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var startTime = DateTime.UtcNow;
    
    await next();
    
    var duration = DateTime.UtcNow - startTime;
    logger.LogInformation(
        "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        duration.TotalMilliseconds);
});

app.MapControllers();

// Health check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready") || check.Name == "database"
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

// Seed database with test data in demo/in-memory mode
if (useInMemory || demoMode)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<CleanSyncDbContext>();
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DatabaseSeeder.SeedAsync(dbContext, seederLogger);
}

app.Run();
