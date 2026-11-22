using MicroNamaa.Api.Authorization;

using MicroNamaa.Api.BackgroundServices;

using MicroNamaa.Api.ExceptionHandling;

using MicroNamaa.Api.Extensions;

using MicroNamaa.Api.Hubs;

using MicroNamaa.Api.OpenApi;

using MicroNamaa.Api.Services;

using MicroNamaa.Application;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Settings;

using MicroNamaa.Infrastructure;

using MicroNamaa.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section was not found.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.Configure<PlatformAdminSettings>(builder.Configuration.GetSection(PlatformAdminSettings.SectionName));

builder.Services.Configure<PlatformSettings>(builder.Configuration.GetSection(PlatformSettings.SectionName));

builder.Services.Configure<SampleDataSettings>(builder.Configuration.GetSection(SampleDataSettings.SectionName));

builder.Services.Configure<DatabaseSettings>(builder.Configuration.GetSection(DatabaseSettings.SectionName));

builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection(StorageSettings.SectionName));

builder.Services.PostConfigure<StorageSettings>(settings => settings.RootPath = Path.GetFullPath(settings.RootPath, builder.Environment.ContentRootPath));

builder.Services.AddControllers(options => options.Filters.Add<PermissionFilter>());

builder.Services.AddApiResponses();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IUserAppService, UserAppService>();

builder.Services.AddSignalR();

builder.Services.AddSingleton<INotificationPublisher, SignalRNotificationPublisher>();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddJwtAuthentication(jwtSettings);

builder.Services.AddAuthorization();

builder.Services.AddApiRateLimiting();

builder.Services.AddFrontendCors(builder.Configuration);

builder.Services.AddHostedService<MaintenanceWorker>();

var app = builder.Build();

await app.Services.MigrateDatabaseAsync();

await app.Services.SeedPlatformAdminAsync();

await app.Services.SeedSampleDataAsync();

app.UseExceptionHandler();

app.UseApiStatusCodePages();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi();

    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "MicroNamaa API v1"));
}

app.UseHttpsRedirection();

app.UseWebApp();

app.UseCors(CorsExtensions.PolicyName);

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<NotificationsHub>(NotificationsHub.Route);

app.MapWebAppFallback();

app.Run();
