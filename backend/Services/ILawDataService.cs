using Nomopsis.Api.Models;

namespace Nomopsis.Api.Services;

public interface ILawDataService
{
    IReadOnlyCollection<LawSummary> GetLaws();
    LawSummary? GetMetadata(string slug);
    LawXmlFile? FindXmlFile(string slug);
    ParsedLawDocument? GetParsedLaw(string slug);
}
