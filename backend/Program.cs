using System.Net;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var dataPath = Environment.GetEnvironmentVariable("DATA_PATH")
    ?? builder.Configuration["Data:Path"]
    ?? "/data";

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapGet("/api/wetten", () =>
{
    Directory.CreateDirectory(dataPath);
    var laws = Directory.EnumerateFiles(dataPath, "*.xml")
        .Select(ReadLawSummary)
        .OrderBy(law => law.Title)
        .ToArray();

    return Results.Ok(laws);
});

app.MapGet("/api/wetten/{slug}/xml", (string slug) =>
{
    Directory.CreateDirectory(dataPath);
    var file = Directory.EnumerateFiles(dataPath, "*.xml")
        .Select(path => new { Path = path, Summary = ReadLawSummary(path) })
        .FirstOrDefault(item => string.Equals(item.Summary.Slug, slug, StringComparison.OrdinalIgnoreCase));

    return file is null
        ? Results.NotFound(new { message = $"Wet '{slug}' was not found in /data." })
        : Results.File(file.Path, "application/xml; charset=utf-8");
});

app.MapGet("/swagger/v1/swagger.json", () => Results.Json(OpenApiDocument(), new JsonSerializerOptions { WriteIndented = true }));

app.MapGet("/swagger", () => Results.Content(SwaggerHtml(), "text/html; charset=utf-8"));
app.MapGet("/swagger/index.html", () => Results.Content(SwaggerHtml(), "text/html; charset=utf-8"));

app.Run();

LawSummary ReadLawSummary(string path)
{
    try
    {
        using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var doc = XDocument.Load(reader);
        var root = doc.Root;
        var wetgeving = doc.Descendants("wetgeving").FirstOrDefault();
        var bwbId = root?.Attribute("bwb-id")?.Value ?? BwbFromFilename(path);
        var title = DirectText(doc.Descendants("citeertitel").FirstOrDefault())
            ?? DirectText(doc.Descendants("intitule").FirstOrDefault())
            ?? Path.GetFileNameWithoutExtension(path);

        return new LawSummary(
            Slug: bwbId,
            BwbId: bwbId,
            Title: title,
            Kind: wetgeving?.Attribute("soort")?.Value ?? root?.Name.LocalName ?? "wet",
            EffectiveDate: root?.Attribute("inwerkingtreding")?.Value ?? wetgeving?.Attribute("inwerkingtredingsdatum")?.Value ?? "",
            FileName: Path.GetFileName(path),
            XmlUrl: $"/api/wetten/{WebUtility.UrlEncode(bwbId)}/xml");
    }
    catch (Exception ex)
    {
        var slug = BwbFromFilename(path);
        return new LawSummary(slug, slug, Path.GetFileNameWithoutExtension(path), "wet", "", Path.GetFileName(path), $"/api/wetten/{WebUtility.UrlEncode(slug)}/xml", ex.Message);
    }
}

static string? DirectText(XElement? element)
{
    if (element is null) return null;
    var text = string.Join(" ", element.Nodes().OfType<XText>().Select(node => node.Value)).Trim();
    return string.IsNullOrWhiteSpace(text) ? null : text;
}

static string BwbFromFilename(string path)
{
    var name = Path.GetFileNameWithoutExtension(path);
    var match = System.Text.RegularExpressions.Regex.Match(name, "BWBR\\d+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    return match.Success ? match.Value.ToUpperInvariant() : name;
}

static object OpenApiDocument() => new
{
    openapi = "3.0.1",
    info = new { title = "Wet Viewer API", version = "v1" },
    paths = new Dictionary<string, object>
    {
        ["/health"] = new { get = new { summary = "Health check", responses = new { _200 = new { description = "OK" } } } },
        ["/api/wetten"] = new { get = new { summary = "List wetten from /data", responses = new { _200 = new { description = "Laws" } } } },
        ["/api/wetten/{slug}/xml"] = new
        {
            get = new
            {
                summary = "Get raw XML for a Wet",
                parameters = new[] { new { name = "slug", @in = "path", required = true, schema = new { type = "string" } } },
                responses = new { _200 = new { description = "XML" }, _404 = new { description = "Not found" } }
            }
        }
    }
};

static string SwaggerHtml() => """
<!doctype html>
<html>
  <head>
    <meta charset="utf-8">
    <title>Wet Viewer API Swagger</title>
    <style>
      body { font-family: system-ui, sans-serif; margin: 2rem; color: #1e252b; }
      a { color: #176b87; }
      pre { background: #f4f4f2; padding: 1rem; overflow: auto; }
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

public sealed record LawSummary(
    string Slug,
    string BwbId,
    string Title,
    string Kind,
    string EffectiveDate,
    string FileName,
    string XmlUrl,
    string? Error = null);
