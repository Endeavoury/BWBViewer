using WetViewer.Api.Services;

namespace WetViewer.Api.Endpoints;

public static class WettenEndpoints
{
    public static IEndpointRouteBuilder MapWettenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wetten");

        group.MapGet("/", (ILawDataService lawDataService) => Results.Ok(lawDataService.GetLaws()))
            .WithName("ListWetten");

        group.MapGet("/{slug}/xml", (string slug, ILawDataService lawDataService) =>
        {
            var file = lawDataService.FindXmlFile(slug);

            return file is null
                ? Results.NotFound(new { message = $"Wet '{slug}' was not found in /data." })
                : Results.File(file.Path, "application/xml; charset=utf-8");
        }).WithName("GetWetXml");

        return app;
    }
}
