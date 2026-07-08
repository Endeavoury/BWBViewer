using WetViewer.Api.Services;

namespace WetViewer.Api.Endpoints;

public static class WettenEndpoints
{
    public static IEndpointRouteBuilder MapWettenEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/wetten");

        group.MapGet("/", (ILawDataService lawDataService) => Results.Ok(lawDataService.GetLaws()))
            .WithName("ListWetten");

        group.MapGet("/{slug}/xml", (string slug, string? artikel, ILawDataService lawDataService) =>
        {
            var file = lawDataService.FindXmlFile(slug);
            if (file is null) return Results.NotFound(new { message = $"Wet '{slug}' was not found in /data." });
            if (artikel is null) return Results.File(file.Path, "application/xml; charset=utf-8");
            if (!ArticleReference.TryParse(artikel, out var reference))
                return Results.BadRequest(new { message = "Artikel must use the format 47, 47.1, or 47.1a." });

            var filtered = LawArticleFilter.Load(file.Path, reference!);
            return filtered is null
                ? Results.NotFound(new { message = $"Artikel '{artikel}' was not found in Wet '{slug}'." })
                : Results.Text(filtered.ToString(), "application/xml; charset=utf-8");
        }).WithName("GetWetXml");

        group.MapGet("/{slug}/metadata", (string slug, ILawDataService lawDataService) =>
        {
            var metadata = lawDataService.GetMetadata(slug);
            return metadata is null
                ? Results.NotFound(new { message = $"Wet '{slug}' was not found in /data." })
                : Results.Ok(metadata);
        }).WithName("GetWetMetadata");

        group.MapGet("/{slug}/json", (string slug, string? artikel, ILawDataService lawDataService) =>
        {
            var file = lawDataService.FindXmlFile(slug);
            if (file is null) return Results.NotFound(new { message = $"Wet '{slug}' was not found in /data." });
            if (artikel is null) return Results.Ok(LawXmlParser.Parse(file.Path, file.Summary));
            if (!ArticleReference.TryParse(artikel, out var reference))
                return Results.BadRequest(new { message = "Artikel must use the format 47, 47.1, or 47.1a." });

            var filtered = LawArticleFilter.Load(file.Path, reference!);
            var parsedLaw = filtered is null ? null : LawXmlParser.Parse(filtered, file.Summary);
            return parsedLaw is null
                ? Results.NotFound(new { message = $"Artikel '{artikel}' was not found in Wet '{slug}'." })
                : Results.Ok(parsedLaw);
        }).WithName("GetWetJson");

        return app;
    }
}
