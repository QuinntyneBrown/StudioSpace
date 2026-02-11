namespace StudioSpace.Cli.Models;

public sealed class StudioListing
{
    public string Address { get; set; } = string.Empty;
    public string? RentalCost { get; set; }
    public string? PurchaseCost { get; set; }
    public string ListingUrl { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ImageUrls { get; set; } = [];
    public List<string> LocalImagePaths { get; set; } = [];
}
