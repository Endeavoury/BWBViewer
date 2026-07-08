using WetViewer.Api.Models;

namespace WetViewer.Api.Services;

public interface ILawDataService
{
    IReadOnlyCollection<LawSummary> GetLaws();
    LawSummary? GetMetadata(string slug);
    LawXmlFile? FindXmlFile(string slug);
    ParsedLawDocument? GetParsedLaw(string slug);
}
