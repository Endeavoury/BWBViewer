using System.Xml;
using System.Xml.Linq;

namespace Nomopsis.Api.Services;

public static class LawArticleFilter
{
    public static XDocument? Load(string path, ArticleReference reference)
    {
        using var stream = File.OpenRead(path);
        using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        var source = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        var article = source.Descendants("artikel")
            .FirstOrDefault(node => ValueOf(node, "kop", "nr").Equals(reference.Article, StringComparison.OrdinalIgnoreCase));
        if (article is null) return null;

        var filteredArticle = new XElement(article);
        if (reference.Paragraph is not null && !FilterParagraph(filteredArticle, reference)) return null;

        var root = new XElement("toestand",
            source.Root?.Attributes() ?? [],
            new XElement("wetgeving",
                source.Descendants("wetgeving").FirstOrDefault()?.Attributes() ?? [],
                source.Descendants("citeertitel").FirstOrDefault(),
                source.Descendants("intitule").FirstOrDefault(),
                new XElement("wettekst", filteredArticle)));
        return new XDocument(new XDeclaration("1.0", "utf-8", null), root);
    }

    private static bool FilterParagraph(XElement article, ArticleReference reference)
    {
        var paragraph = article.Elements("lid")
            .FirstOrDefault(node => ValueOf(node, "lidnr").Equals(reference.Paragraph, StringComparison.OrdinalIgnoreCase));
        if (paragraph is null) return false;

        article.Elements("lid").Where(node => node != paragraph).Remove();
        if (reference.Subparagraph is null) return true;

        var item = paragraph.Descendants("li")
            .FirstOrDefault(node => NormalizeSubparagraph(ValueOf(node, "li.nr")) == reference.Subparagraph);
        if (item is null) return false;

        var selected = new XElement(item);
        paragraph.Elements().Where(node => node.Name.LocalName is not "lidnr").Remove();
        paragraph.Add(new XElement("lijst", selected));
        return true;
    }

    private static string ValueOf(XElement node, params string[] path)
    {
        XElement? current = node;
        foreach (var name in path) current = current?.Elements(name).FirstOrDefault();
        return current?.Value.Trim() ?? "";
    }

    private static string NormalizeSubparagraph(string value) => value.Trim().TrimEnd('.').ToLowerInvariant();
}
