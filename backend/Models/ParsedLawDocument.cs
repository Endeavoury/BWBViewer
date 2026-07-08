namespace WetViewer.Api.Models;

public sealed record ParsedLawDocument(
    string BwbId,
    string Kind,
    string Inwerking,
    string ShortTitle,
    string? LongTitle,
    IReadOnlyCollection<LawSection> Sections,
    IReadOnlyCollection<TocEntry> Toc,
    LawStats Stats);

public sealed record LawStats(int Articles, int Chapters);

public sealed record TocEntry(string Id, string Title, string Kind, int Depth);

public sealed record LawSection(
    string Type,
    string Id,
    int Level,
    string NodeName,
    string Title,
    string? ArticleNumber,
    string? Reference,
    bool IsAuthority,
    IReadOnlyCollection<LawXmlNode> BodyNodes,
    IReadOnlyCollection<LawSection> Children,
    string? XmlId = null);

public sealed record LawXmlNode(
    string LocalName,
    string Text,
    IReadOnlyDictionary<string, string> Attributes,
    IReadOnlyCollection<LawXmlNode> Children);
