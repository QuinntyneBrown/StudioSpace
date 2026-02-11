using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudioSpace.Cli.Services;

namespace StudioSpace.Cli.Commands;

public sealed class SearchStudiosCommand : Command
{
    public static readonly Option<string> PostalCodeOption = new("--postal-code", "-p")
    {
        Description = "Postal code to search near",
        DefaultValueFactory = _ => "L5A 4E6"
    };

    public static readonly Option<string> OutputOption = new("--output", "-o")
    {
        Description = "Output directory for the report and images",
        DefaultValueFactory = _ => "artifacts"
    };

    public static readonly Option<int> MaxListingsOption = new("--max-listings", "-m")
    {
        Description = "Maximum number of listings to include",
        DefaultValueFactory = _ => 20
    };

    public SearchStudiosCommand() : base("search", "Search for photography studio space for rent or sale")
    {
        Options.Add(PostalCodeOption);
        Options.Add(OutputOption);
        Options.Add(MaxListingsOption);
    }
}

public sealed class SearchStudiosHandler : AsynchronousCommandLineAction
{
    private readonly IServiceProvider _services;

    public SearchStudiosHandler(IServiceProvider services)
    {
        _services = services;
    }

    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken ct = default)
    {
        var logger = _services.GetRequiredService<ILogger<SearchStudiosHandler>>();
        var searchService = _services.GetRequiredService<ISearchService>();
        var imageDownloader = _services.GetRequiredService<IImageDownloader>();
        var reportGenerator = _services.GetRequiredService<IReportGenerator>();

        var postalCode = parseResult.GetValue(SearchStudiosCommand.PostalCodeOption) ?? "L5A 4E6";
        var outputDir = parseResult.GetValue(SearchStudiosCommand.OutputOption) ?? "artifacts";
        var maxListings = parseResult.GetValue(SearchStudiosCommand.MaxListingsOption);

        logger.LogInformation("=== Studio Space Finder ===");
        logger.LogInformation("Searching for photography studio space near {PostalCode}...", postalCode);
        logger.LogInformation("Output directory: {OutputDir}", Path.GetFullPath(outputDir));

        try
        {
            // Step 1: Search
            var listings = await searchService.SearchListingsAsync(postalCode, ct);

            if (listings.Count == 0)
            {
                logger.LogWarning("No listings found. Try broadening your search.");
                return 1;
            }

            if (listings.Count > maxListings)
                listings = listings.Take(maxListings).ToList();

            logger.LogInformation("Processing {Count} listings...", listings.Count);

            // Step 2: Download images
            await imageDownloader.DownloadImagesAsync(listings, outputDir, ct);

            // Step 3: Generate report
            var reportPath = await reportGenerator.GenerateReportAsync(listings, outputDir, ct);

            logger.LogInformation("=== Complete ===");
            logger.LogInformation("Report: {Path}", Path.GetFullPath(reportPath));
            logger.LogInformation("Images: {Path}", Path.GetFullPath(Path.Combine(outputDir, "images")));

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Search failed");
            return 1;
        }
    }
}
