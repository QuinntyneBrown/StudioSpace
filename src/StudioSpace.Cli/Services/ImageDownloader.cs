using Microsoft.Extensions.Logging;
using StudioSpace.Cli.Models;

namespace StudioSpace.Cli.Services;

public interface IImageDownloader
{
    Task DownloadImagesAsync(List<StudioListing> listings, string outputDir, CancellationToken ct = default);
}

public sealed class ImageDownloader : IImageDownloader
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ImageDownloader> _logger;

    public ImageDownloader(HttpClient httpClient, ILogger<ImageDownloader> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task DownloadImagesAsync(List<StudioListing> listings, string outputDir, CancellationToken ct = default)
    {
        var imagesDir = Path.Combine(outputDir, "images");
        Directory.CreateDirectory(imagesDir);

        int listingIndex = 0;
        foreach (var listing in listings)
        {
            listingIndex++;
            int imgIndex = 0;
            foreach (var imageUrl in listing.ImageUrls)
            {
                imgIndex++;
                try
                {
                    var extension = GetImageExtension(imageUrl);
                    var fileName = $"listing_{listingIndex:D2}_img_{imgIndex:D2}{extension}";
                    var filePath = Path.Combine(imagesDir, fileName);

                    using var response = await _httpClient.GetAsync(imageUrl, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
                        if (bytes.Length > 1000) // skip tiny/broken images
                        {
                            await File.WriteAllBytesAsync(filePath, bytes, ct);
                            listing.LocalImagePaths.Add(Path.Combine("images", fileName));
                            _logger.LogDebug("Downloaded {Url} -> {Path}", imageUrl, fileName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to download image {Url}", imageUrl);
                }
            }
        }

        _logger.LogInformation("Downloaded images to {Dir}", imagesDir);
    }

    private static string GetImageExtension(string url)
    {
        var lower = url.ToLowerInvariant();
        if (lower.Contains(".png")) return ".png";
        if (lower.Contains(".webp")) return ".webp";
        if (lower.Contains(".gif")) return ".gif";
        return ".jpg";
    }
}
