using Microsoft.AspNetCore.StaticFiles;

namespace MicroNamaa.Api.Extensions;

/// <summary>
/// Serves the web app (the frontend's build in wwwroot) from the same site as the API,
/// so the whole platform opens from one address. Does nothing when wwwroot has no index.html.
/// </summary>
public static class WebAppExtensions
{
    private const string IndexFile = "index.html";

    // Every path the API owns; anything else that is not a file is an Angular route.
    private const string AppRoutes = "{*path:nonfile:regex(^(?!api/|hubs/|openapi/|swagger).*$)}";

    public static WebApplication UseWebApp(this WebApplication app)
    {
        if (!HasWebApp(app))
        {
            return app;
        }

        var contentTypes = new FileExtensionContentTypeProvider();

        contentTypes.Mappings[".webmanifest"] = "application/manifest+json";

        app.UseDefaultFiles();

        app.UseStaticFiles(new StaticFileOptions
        {
            ContentTypeProvider = contentTypes,

            OnPrepareResponse = context => SetCacheHeaders(context.Context.Response, context.File.Name)
        });

        return app;
    }

    public static WebApplication MapWebAppFallback(this WebApplication app)
    {
        if (HasWebApp(app))
        {
            app.MapFallbackToFile(AppRoutes, IndexFile, new StaticFileOptions
            {
                OnPrepareResponse = context => SetCacheHeaders(context.Context.Response, IndexFile)
            });
        }

        return app;
    }

    private static bool HasWebApp(WebApplication app)
    {
        var root = app.Environment.WebRootPath;

        return !string.IsNullOrEmpty(root) && File.Exists(Path.Combine(root, IndexFile));
    }

    // index.html must always be fresh so a new deploy shows at once; the hashed bundles can be cached.
    private static void SetCacheHeaders(HttpResponse response, string fileName)
    {
        response.Headers.CacheControl = fileName == IndexFile ? "no-cache" : "public, max-age=604800";
    }
}
