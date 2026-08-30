using Nomopsis.Api.Models;

namespace Nomopsis.Api.Services;

public sealed record LawXmlFile(string Path, LawSummary Summary);
