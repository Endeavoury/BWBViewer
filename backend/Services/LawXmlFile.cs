using WetViewer.Api.Models;

namespace WetViewer.Api.Services;

public sealed record LawXmlFile(string Path, LawSummary Summary);
