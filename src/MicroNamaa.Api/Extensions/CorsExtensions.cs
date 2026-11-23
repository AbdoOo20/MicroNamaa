namespace MicroNamaa.Api.Extensions;

public static class CorsExtensions
{
    public const string PolicyName = "Frontend";

    public static IServiceCollection AddFrontendCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        // The desktop app (Electron/Tauri) calls from file://, app:// or tauri:// origins. Sign-in uses bearer tokens, not cookies.
        var allowAnyOrigin = configuration.GetValue<bool>("Cors:AllowAnyOrigin");

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (allowAnyOrigin)
                {
                    policy.SetIsOriginAllowed(_ => true)

                        .AllowAnyHeader()

                        .AllowAnyMethod()

                        .AllowCredentials()

                        .WithExposedHeaders("Content-Disposition");
                }

                else if (origins.Length > 0)
                {
                    policy.WithOrigins(origins)

                        .AllowAnyHeader()

                        .AllowAnyMethod()

                        .AllowCredentials()

                        .WithExposedHeaders("Content-Disposition");
                }
            });
        });

        return services;
    }
}
