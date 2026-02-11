using StudioSpace.Cli.Models;

namespace StudioSpace.Cli.Services;

public interface IReportGenerator
{
    Task<string> GenerateReportAsync(List<StudioListing> listings, string outputDir, CancellationToken ct = default);
}
