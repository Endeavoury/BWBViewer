using System.Text.RegularExpressions;

namespace WetViewer.Api.Services;

public sealed partial record ArticleReference(string Article, string? Paragraph, string? Subparagraph)
{
    public static bool TryParse(string? value, out ArticleReference? reference)
    {
        reference = null;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var match = ReferenceRegex().Match(value.Trim());
        if (!match.Success) return false;

        reference = new ArticleReference(
            match.Groups["article"].Value,
            GroupValue(match, "paragraph"),
            GroupValue(match, "subparagraph")?.ToLowerInvariant());
        return true;
    }

    private static string? GroupValue(Match match, string name) =>
        match.Groups[name].Success ? match.Groups[name].Value : null;

    [GeneratedRegex(@"^(?<article>\d+[a-z]?)(?:\.(?<paragraph>\d+)(?<subparagraph>[a-z])?)?$", RegexOptions.IgnoreCase)]
    private static partial Regex ReferenceRegex();
}
