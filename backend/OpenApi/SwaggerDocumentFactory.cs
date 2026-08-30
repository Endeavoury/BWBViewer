namespace Nomopsis.Api.OpenApi;

public static class SwaggerDocumentFactory
{
    public static object Create() => new
    {
        openapi = "3.0.1",
        info = new
        {
            title = "Nomopsis API",
            version = "v1",
            description = "API voor het opvragen van Nederlandse wetten uit de gekoppelde /data-map."
        },
        paths = new Dictionary<string, object>
        {
            ["/health"] = new
            {
                get = new
                {
                    summary = "Controleer de beschikbaarheid van de API",
                    description = "Geeft aan of de backend actief en bereikbaar is.",
                    responses = ResponseDescriptions(("200", "De API is beschikbaar."))
                }
            },
            ["/api/wetten"] = new
            {
                get = new
                {
                    summary = "Haal alle wetten op",
                    description = "Geeft een overzicht van alle XML-wetten in de gekoppelde /data-map.",
                    responses = ResponseDescriptions(("200", "Overzicht van beschikbare wetten."))
                }
            },
            ["/api/wetten/{slug}/xml"] = new
            {
                get = new
                {
                    summary = "Haal een wet op als XML",
                    description = "Geeft de oorspronkelijke XML terug. Gebruik de optionele parameter artikel om alleen een artikel, lid of sublid op te halen.",
                    parameters = LawContentParameters(),
                    responses = ResponseDescriptions(
                        ("200", "De wet of het geselecteerde onderdeel als XML."),
                        ("400", "De artikelverwijzing heeft een ongeldig formaat."),
                        ("404", "De wet of het gevraagde onderdeel is niet gevonden."))
                }
            },
            ["/api/wetten/{slug}/metadata"] = new
            {
                get = new
                {
                    summary = "Haal metadata van een wet op",
                    description = "Geeft beknopte informatie over de wet, zoals titel, BWB-nummer en ingangsdatum.",
                    parameters = SlugParameters(),
                    responses = ResponseDescriptions(
                        ("200", "Metadata van de wet."),
                        ("404", "De wet is niet gevonden."))
                }
            },
            ["/api/wetten/{slug}/json"] = new
            {
                get = new
                {
                    summary = "Haal een wet op als JSON",
                    description = "Geeft de geparseerde structuur van de wet terug. Gebruik de optionele parameter artikel om alleen een artikel, lid of sublid op te halen.",
                    parameters = LawContentParameters(),
                    responses = ResponseDescriptions(
                        ("200", "De geparseerde wet of het geselecteerde onderdeel als JSON."),
                        ("400", "De artikelverwijzing heeft een ongeldig formaat."),
                        ("404", "De wet of het gevraagde onderdeel is niet gevonden."))
                }
            }
        }
    };

    public static string Html() => """
    <!doctype html>
    <html>
      <head>
        <meta charset="utf-8">
        <title>Nomopsis API-documentatie</title>
        <link rel="stylesheet" href="https://unpkg.com/swagger-ui-dist@5/swagger-ui.css">
      </head>
      <body>
        <div id="swagger-ui"></div>
        <script src="https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js"></script>
        <script>
          window.ui = SwaggerUIBundle({ url: '/swagger/v1/swagger.json', dom_id: '#swagger-ui' });
        </script>
      </body>
    </html>
    """;

    private static object[] SlugParameters() =>
    [
        new
        {
            name = "slug",
            @in = "path",
            required = true,
            description = "Het BWB-nummer van de wet, bijvoorbeeld BWBR0039896.",
            schema = new { type = "string" },
            example = "BWBR0039896"
        }
    ];

    private static object[] LawContentParameters() =>
    [
        new
        {
            name = "slug",
            @in = "path",
            required = true,
            description = "Het BWB-nummer van de wet, bijvoorbeeld BWBR0039896.",
            schema = new { type = "string" },
            example = "BWBR0039896"
        },
        new
        {
            name = "artikel",
            @in = "query",
            required = false,
            description = "Optionele artikelverwijzing. Gebruik bijvoorbeeld 47 voor een artikel of 47.1 voor een lid.",
            schema = new { type = "string", pattern = @"^\d+[a-z]?(\.\d+[a-z]?)?$" },
            example = "47.1"
        }
    ];

    private static Dictionary<string, object> ResponseDescriptions(params (string Status, string Description)[] responses)
    {
        return responses.ToDictionary(
            response => response.Status,
            response => (object)new { description = response.Description });
    }
}
