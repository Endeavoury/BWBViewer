using WetViewer.Api.Services;

var tests = new (string Name, Action Test)[]
{
    ("BWB slug is detected from filename", BwbSlugIsDetectedFromFilename),
    ("XML parser extracts article anchors and inline text", XmlParserExtractsArticleAnchorsAndInlineText),
    ("Article references support article, paragraph, and subparagraph", ArticleReferencesAreParsed),
    ("XML article filtering selects the requested legal part", XmlArticleFilteringSelectsRequestedPart)
};

var failures = new List<string>();

foreach (var (name, test) in tests)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception ex)
    {
        failures.Add($"{name}: {ex.Message}");
        Console.WriteLine($"FAIL {name}: {ex.Message}");
    }
}

if (failures.Count > 0)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine("Failures:");
    failures.ForEach(Console.Error.WriteLine);
    Environment.Exit(1);
}

static void BwbSlugIsDetectedFromFilename()
{
    AssertEqual("BWBR0049562", LawXmlParser.BwbFromFilename("BWBR0049562_2024-07-01_0.xml"));
    AssertEqual("example", LawXmlParser.BwbFromFilename("example.xml"));
}

static void XmlParserExtractsArticleAnchorsAndInlineText()
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"wet-{Guid.NewGuid():N}.xml");
    File.WriteAllText(tempFile, """
    <?xml version="1.0" encoding="UTF-8"?>
    <toestand bwb-id="BWBR0099999" inwerkingtreding="2026-01-01">
      <wetgeving soort="wet">
        <citeertitel>Testwet</citeertitel>
        <wettekst>
          <hoofdstuk id="h1">
            <kop><label>Hoofdstuk</label><nr>1</nr><titel>Bijzondere bevoegdheden</titel></kop>
            <artikel id="a6" label="Artikel 6">
              <kop><label>Artikel</label><nr>6</nr></kop>
              <lid><lidnr>6</lidnr><al>Voor <nadruk type="cur">test</nadruk> tekst.</al></lid>
            </artikel>
          </hoofdstuk>
        </wettekst>
      </wetgeving>
    </toestand>
    """);

    try
    {
        var summary = new WetViewer.Api.Models.LawSummary("BWBR0099999", "BWBR0099999", "Testwet", "wet", "2026-01-01", Path.GetFileName(tempFile), "/api/wetten/BWBR0099999/xml");
        var parsed = LawXmlParser.Parse(tempFile, summary);
        var article = parsed.Sections.Single().Children.Single();
        AssertEqual("artikel-6", article.Id);
        AssertTrue(article.IsAuthority, "Article below bevoegdheden context should be marked as authority.");

        var lid = article.BodyNodes.Single(node => node.LocalName == "lid");
        var al = lid.Children.Single(node => node.LocalName == "al");
        AssertTrue(al.Children.Any(node => node.LocalName == "#text" && node.Text.Contains("Voor")), "Inline leading text should be preserved.");
        AssertTrue(al.Children.Any(node => node.LocalName == "nadruk" && node.Text == "test"), "Inline emphasis should be preserved.");
    }
    finally
    {
        File.Delete(tempFile);
    }
}

static void ArticleReferencesAreParsed()
{
    AssertReference("47", "47", null, null);
    AssertReference("47.1", "47", "1", null);
    AssertReference("47.1a", "47", "1", "a");
    AssertTrue(!ArticleReference.TryParse("47.a", out _), "Malformed references should be rejected.");
}

static void AssertReference(string value, string article, string? paragraph, string? subparagraph)
{
    AssertTrue(ArticleReference.TryParse(value, out var reference), $"'{value}' should be accepted.");
    AssertEqual(article, reference!.Article);
    AssertEqual(paragraph, reference.Paragraph);
    AssertEqual(subparagraph, reference.Subparagraph);
}

static void XmlArticleFilteringSelectsRequestedPart()
{
    var tempFile = Path.Combine(Path.GetTempPath(), $"wet-filter-{Guid.NewGuid():N}.xml");
    File.WriteAllText(tempFile, """
    <toestand bwb-id="BWBR0099999"><wetgeving soort="wet"><citeertitel>Testwet</citeertitel><wettekst>
      <artikel><kop><label>Artikel</label><nr>47</nr></kop>
        <lid><lidnr>1</lidnr><lijst><li><li.nr>a.</li.nr><al>Eerste.</al></li><li><li.nr>b.</li.nr><al>Tweede.</al></li></lijst></lid>
        <lid><lidnr>2</lidnr><al>Ander lid.</al></lid>
      </artikel>
    </wettekst></wetgeving></toestand>
    """);

    try
    {
        var filtered = LawArticleFilter.Load(tempFile, new ArticleReference("47", "1", "a"));
        AssertTrue(filtered is not null, "Requested subparagraph should be found.");
        AssertEqual(1, filtered!.Descendants("lid").Count());
        AssertEqual("1", filtered.Descendants("lidnr").Single().Value);
        AssertEqual(1, filtered.Descendants("li").Count());
        AssertEqual("a.", filtered.Descendants("li.nr").Single().Value);
    }
    finally
    {
        File.Delete(tempFile);
    }
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"Expected '{expected}', got '{actual}'.");
    }
}

static void AssertTrue(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}
