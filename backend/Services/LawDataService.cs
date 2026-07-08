using System.Net;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using WetViewer.Api.Models;

namespace WetViewer.Api.Services;

public sealed partial class LawDataService(IDataPathProvider dataPathProvider) : ILawDataService
{
    private readonly string _dataPath = dataPathProvider.DataPath;

    public IReadOnlyCollection<LawSummary> GetLaws()
    {
        Directory.CreateDirectory(_dataPath);

        return Directory.EnumerateFiles(_dataPath, "*.xml")
            .Select(ReadLawSummary)
            .OrderBy(law => law.Title)
            .ToArray();
    }

    public LawXmlFile? FindXmlFile(string slug)
    {
        Directory.CreateDirectory(_dataPath);

        return Directory.EnumerateFiles(_dataPath, "*.xml")
            .Select(path => new LawXmlFile(path, ReadLawSummary(path)))
            .FirstOrDefault(file => string.Equals(file.Summary.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }

    private static LawSummary ReadLawSummary(string path)
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
            return new LawSummary(
                Slug: slug,
                BwbId: slug,
                Title: Path.GetFileNameWithoutExtension(path),
                Kind: "wet",
                EffectiveDate: "",
                FileName: Path.GetFileName(path),
                XmlUrl: $"/api/wetten/{WebUtility.UrlEncode(slug)}/xml",
                Error: ex.Message);
        }
    }

    private static string? DirectText(XElement? element)
    {
        if (element is null) return null;
        var text = string.Join(" ", element.Nodes().OfType<XText>().Select(node => node.Value)).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string BwbFromFilename(string path)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var match = BwbIdRegex().Match(name);
        return match.Success ? match.Value.ToUpperInvariant() : name;
    }

    [GeneratedRegex("BWBR\\d+", RegexOptions.IgnoreCase)]
    private static partial Regex BwbIdRegex();
}
