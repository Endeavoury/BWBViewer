using System.Text.Json;
using Nomopsis.Api.OpenApi;

namespace Nomopsis.Api.Endpoints;

public static class SwaggerEndpoints
{
    public static IEndpointRouteBuilder MapSwaggerEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/swagger/v1/swagger.json", () =>
            Results.Json(SwaggerDocumentFactory.Create(), new JsonSerializerOptions { WriteIndented = true }));

        app.MapGet("/swagger", () => Results.Content(SwaggerDocumentFactory.Html(), "text/html; charset=utf-8"));
        app.MapGet("/swagger/index.html", () => Results.Content(SwaggerDocumentFactory.Html(), "text/html; charset=utf-8"));

        return app;
    }
}
