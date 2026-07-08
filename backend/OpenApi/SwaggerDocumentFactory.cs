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
                    summary = "Get raw XML for a Wet",
                    parameters = new[] { new { name = "slug", @in = "path", required = true, schema = new { type = "string" } } },
                    responses = ResponseDescriptions(("200", "XML"), ("404", "Not found"))
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
        <style>
          body { font-family: system-ui, sans-serif; margin: 2rem; color: #1e252b; }
          a { color: #176b87; }
          code { background: #f4f4f2; padding: 0.1rem 0.3rem; }
        </style>
      </head>
      <body>
        <h1>Wet Viewer API</h1>
        <p>OpenAPI JSON: <a href="/swagger/v1/swagger.json">/swagger/v1/swagger.json</a></p>
        <h2>Endpoints</h2>
        <ul>
          <li><code>GET /api/wetten</code></li>
          <li><code>GET /api/wetten/{slug}/xml</code></li>
          <li><code>GET /health</code></li>
        </ul>
      </body>
    </html>
    """;

    private static Dictionary<string, object> ResponseDescriptions(params (string Status, string Description)[] responses)
    {
        return responses.ToDictionary(
            response => response.Status,
            response => (object)new { description = response.Description });
    }
}
