using StudioSpace.Cli.Models;

namespace StudioSpace.Cli.Services;

public interface ISearchService
{
    Task<List<StudioListing>> SearchListingsAsync(string postalCode, CancellationToken ct = default);
}
