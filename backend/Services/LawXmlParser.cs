using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Nomopsis.Api.Models;

namespace Nomopsis.Api.Services;

public static partial class LawXmlParser
{
    public static ParsedLawDocument Parse(string xmlPath, LawSummary summary)
    {
        using var stream = File.OpenRead(xmlPath);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var doc = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        return Parse(doc, summary);
    }

    public static ParsedLawDocument Parse(XDocument doc, LawSummary summary)
    {
        var root = doc.Root;
        var wetgeving = doc.Descendants("wetgeving").FirstOrDefault();
        var shortTitle = DirectText(doc.Descendants("citeertitel").FirstOrDefault())
            ?? DirectText(doc.Descendants("intitule").FirstOrDefault())
            ?? summary.Title;
        var longTitle = DirectText(doc.Descendants("intitule").FirstOrDefault());
        var wettekst = doc.Descendants("wettekst").FirstOrDefault();
        var source = wettekst ?? wetgeving ?? root;
        var sections = source?.Elements()
            .Where(node => !IgnoredTopLevel.Contains(node.Name.LocalName))
            .Select((node, index) => BuildSection(node, index, []))
            .Where(section => section is not null)
            .Cast<LawSection>()
            .ToArray() ?? [];
        var toc = new List<TocEntry>();
        CollectToc(sections, toc, 0);

        return new ParsedLawDocument(
            BwbId: summary.BwbId,
            Kind: summary.Kind,
            Inwerking: summary.EffectiveDate,
            ShortTitle: shortTitle,
            LongTitle: longTitle,
            Sections: sections,
            Toc: toc,
            Stats: new LawStats(
                Articles: doc.Descendants("artikel").Count(),
                Chapters: doc.Descendants("hoofdstuk").Count()));
    }

    public static string BwbFromFilename(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var match = BwbIdRegex().Match(name);
        return match.Success ? match.Value.ToUpperInvariant() : name;
    }

    public static string? DirectText(XElement? element)
    {
        if (element is null) return null;
        var text = string.Join(" ", element.Nodes().OfType<XText>().Select(node => node.Value)).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : WhitespaceRegex().Replace(text, " ");
    }

    private static LawSection? BuildSection(XElement node, int index, IReadOnlyCollection<string> context)
    {
        if (node.Name.LocalName == "artikel")
        {
            return BuildArticle(node, index, context);
        }

        var children = node.Elements()
            .Where(child => child.Name.LocalName is not "kop" and not "meta-data")
            .ToArray();
        var title = HeadingText(node) ?? ReadableName(node.Name.LocalName);
        var nextContext = context.Append(title).ToArray();
        var nestedSections = children
            .Where(child => child.Name.LocalName == "artikel" || IsContainer(child))
            .Select((child, childIndex) => child.Name.LocalName == "artikel"
                ? BuildArticle(child, childIndex, nextContext)
                : BuildSection(child, childIndex, nextContext))
            .Where(section => section is not null)
            .Cast<LawSection>()
            .ToArray();

        return new LawSection(
            Type: "section",
            Id: SafeId(node.Attribute("id")?.Value ?? $"section-{index}"),
            Level: SectionLevel(node.Name.LocalName),
            NodeName: node.Name.LocalName,
            Title: title,
            ArticleNumber: null,
            Reference: null,
            IsAuthority: false,
            BodyNodes: children
                .Where(child => child.Name.LocalName != "artikel" && !IsContainer(child))
                .Select(ToXmlNode)
                .ToArray(),
            Children: nestedSections);
    }

    private static LawSection BuildArticle(XElement node, int index, IReadOnlyCollection<string> context)
    {
        var articleNumber = ArticleNumberFromNode(node);
        var title = HeadingText(node) ?? node.Attribute("label")?.Value ?? "Artikel";
        return new LawSection(
            Type: "article",
            Id: articleNumber is not null ? LegalAnchor(["artikel", articleNumber]) : SafeId(node.Attribute("id")?.Value ?? $"article-{index}"),
            Level: 0,
            NodeName: "artikel",
            Title: title,
            ArticleNumber: articleNumber,
            Reference: articleNumber is not null ? $"Artikel {articleNumber}" : title,
            IsAuthority: context.Any(part => NormalizeText(part).Contains("bevoegdhed")),
            BodyNodes: node.Elements()
                .Where(child => child.Name.LocalName is not "kop" and not "meta-data")
                .Select(ToXmlNode)
                .ToArray(),
            Children: [],
            XmlId: node.Attribute("id")?.Value);
    }

    private static LawXmlNode ToXmlNode(XElement node)
    {
        return new LawXmlNode(
            LocalName: node.Name.LocalName,
            Text: TextOf(node),
            Attributes: node.Attributes().ToDictionary(attribute => attribute.Name.LocalName, attribute => attribute.Value),
            Children: node.Nodes()
                .Select(ToXmlNode)
                .Where(child => child is not null)
                .Cast<LawXmlNode>()
                .ToArray());
    }

    private static LawXmlNode? ToXmlNode(XNode node)
    {
        return node switch
        {
            XElement element => ToXmlNode(element),
            XText text when !string.IsNullOrWhiteSpace(text.Value) => new LawXmlNode(
                LocalName: "#text",
                Text: WhitespaceRegex().Replace(text.Value, " "),
                Attributes: new Dictionary<string, string>(),
                Children: []),
            _ => null
        };
    }

    private static void CollectToc(IEnumerable<LawSection> sections, ICollection<TocEntry> entries, int depth)
    {
        foreach (var section in sections)
        {
            entries.Add(new TocEntry(section.Id, section.Title, section.Type == "article" ? "Artikel" : ReadableName(section.NodeName), depth));
            CollectToc(section.Children, entries, depth + 1);
        }
    }

    private static string? HeadingText(XElement node)
    {
        var kop = node.Elements().FirstOrDefault(child => child.Name.LocalName == "kop");
        if (kop is null) return node.Attribute("label")?.Value;
        var label = DirectText(kop.Elements().FirstOrDefault(child => child.Name.LocalName == "label"));
        var nr = DirectText(kop.Elements().FirstOrDefault(child => child.Name.LocalName == "nr"));
        var title = DirectText(kop.Elements().FirstOrDefault(child => child.Name.LocalName == "titel"));
        return string.Join(" ", new[] { label, nr, title }.Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string? ArticleNumberFromNode(XElement node)
    {
        var kop = node.Elements().FirstOrDefault(child => child.Name.LocalName == "kop");
        var nr = DirectText(kop?.Elements().FirstOrDefault(child => child.Name.LocalName == "nr"));
        if (!string.IsNullOrWhiteSpace(nr)) return nr;
        var label = node.Attribute("label")?.Value ?? "";
        var match = ArticleNumberRegex().Match(label);
        return match.Success ? match.Value : null;
    }

    private static bool IsContainer(XElement node) => Containers.Contains(node.Name.LocalName);

    private static int SectionLevel(string name) => name switch
    {
        "hoofdstuk" => 1,
        "paragraaf" => 2,
        "sub-paragraaf" => 3,
        "afdeling" => 2,
        "titeldeel" => 1,
        _ => 2
    };

    private static string TextOf(XElement node) => WhitespaceRegex().Replace(node.Value, " ").Trim();
    private static string ReadableName(string name) => string.Join(" ", name.Split('-').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    private static string SafeId(string id) => UnsafeIdRegex().Replace(id, "-");
    private static string LegalAnchor(IEnumerable<string?> parts) => string.Join("-", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(NormalizeLegalPart));
    private static string NormalizeText(string value) => WhitespaceRegex().Replace(value.ToLowerInvariant(), " ").Trim();
    private static string NormalizeLegalPart(string? value) => UnsafeLegalPartRegex().Replace((value ?? "").Trim().Replace("\u00b0", "").TrimEnd('.').Replace(' ', '-'), "").ToLowerInvariant();

    private static readonly HashSet<string> IgnoredTopLevel = ["meta-data", "citeertitel", "intitule"];
    private static readonly HashSet<string> Containers = ["hoofdstuk", "paragraaf", "sub-paragraaf", "afdeling", "titeldeel"];

    [GeneratedRegex("BWBR\\d+", RegexOptions.IgnoreCase)]
    private static partial Regex BwbIdRegex();

    [GeneratedRegex("\\d+[a-z]?", RegexOptions.IgnoreCase)]
    private static partial Regex ArticleNumberRegex();

    [GeneratedRegex("[^a-zA-Z0-9_-]")]
    private static partial Regex UnsafeIdRegex();

    [GeneratedRegex("[^a-zA-Z0-9.-]")]
    private static partial Regex UnsafeLegalPartRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();
}
