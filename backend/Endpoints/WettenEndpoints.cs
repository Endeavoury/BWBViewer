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

        group.MapGet("/{slug}/metadata", (string slug, ILawDataService lawDataService) =>
        {
            var metadata = lawDataService.GetMetadata(slug);
            return metadata is null
                ? Results.NotFound(new { message = $"Wet '{slug}' was not found in /data." })
                : Results.Ok(metadata);
        }).WithName("GetWetMetadata");

        group.MapGet("/{slug}/json", (string slug, ILawDataService lawDataService) =>
        {
            var parsedLaw = lawDataService.GetParsedLaw(slug);
            return parsedLaw is null
                ? Results.NotFound(new { message = $"Wet '{slug}' was not found in /data." })
                : Results.Ok(parsedLaw);
        }).WithName("GetWetJson");

        return app;
    }
}
