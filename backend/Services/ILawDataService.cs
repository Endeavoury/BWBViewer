using WetViewer.Api.Models;

namespace WetViewer.Api.Services;

public interface ILawDataService
{
    IReadOnlyCollection<LawSummary> GetLaws();
    LawXmlFile? FindXmlFile(string slug);
}
