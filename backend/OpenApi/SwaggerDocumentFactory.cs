namespace WetViewer.Api.OpenApi;

public static class SwaggerDocumentFactory
{
    public static object Create() => new
    {
        openapi = "3.0.1",
        info = new { title = "Wet Viewer API", version = "v1" },
        paths = new Dictionary<string, object>
        {
            ["/health"] = new
            {
                get = new
                {
                    summary = "Health check",
                    responses = ResponseDescriptions(("200", "OK"))
                }
            },
            ["/api/wetten"] = new
            {
                get = new
                {
                    summary = "List wetten from /data",
                    responses = ResponseDescriptions(("200", "Laws"))
                }
            },
            ["/api/wetten/{slug}/xml"] = new
            {
                get = new
                {
                    summary = "Get XML for a Wet, optionally filtered by article reference",
                    parameters = LawContentParameters(),
                    responses = ResponseDescriptions(("200", "XML"), ("400", "Invalid article reference"), ("404", "Not found"))
                }
            },
            ["/api/wetten/{slug}/metadata"] = new
            {
                get = new
                {
                    summary = "Get metadata for a Wet",
                    parameters = SlugParameters(),
                    responses = ResponseDescriptions(("200", "Metadata"), ("404", "Not found"))
                }
            },
            ["/api/wetten/{slug}/json"] = new
            {
                get = new
                {
                    summary = "Get parsed JSON for a Wet, optionally filtered by article reference",
                    parameters = LawContentParameters(),
                    responses = ResponseDescriptions(("200", "Parsed law"), ("400", "Invalid article reference"), ("404", "Not found"))
                }
            }
        }
    };

    public static string Html() => """
    <!doctype html>
    <html>
      <head>
        <meta charset="utf-8">
        <title>Wet Viewer API Swagger</title>
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

    private static object[] SlugParameters() => [new { name = "slug", @in = "path", required = true, schema = new { type = "string" } }];

    private static object[] LawContentParameters() =>
    [
        new { name = "slug", @in = "path", required = true, schema = new { type = "string" } },
        new { name = "artikel", @in = "query", required = false, description = "Examples: 47, 47.1, 47.1a", schema = new { type = "string", pattern = @"^\d+[a-z]?(\.\d+[a-z]?)?$" } }
    ];

    private static Dictionary<string, object> ResponseDescriptions(params (string Status, string Description)[] responses)
    {
        return responses.ToDictionary(
            response => response.Status,
            response => (object)new { description = response.Description });
    }
}
