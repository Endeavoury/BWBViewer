namespace Nomopsis.Api.Models;

public sealed record LawSummary(
    string Slug,
    string BwbId,
    string Title,
    string Kind,
    string EffectiveDate,
    string FileName,
    string XmlUrl,
    string? Error = null);
