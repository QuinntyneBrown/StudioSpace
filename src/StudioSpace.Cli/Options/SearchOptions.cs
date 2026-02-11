namespace StudioSpace.Cli.Options;

public sealed class SearchOptions
{
    public const string SectionName = "Search";

    public string PostalCode { get; set; } = "L5A 4E6";
    public string ArtifactsPath { get; set; } = "artifacts";
    public int MaxListings { get; set; } = 20;
    public int ImageDownloadTimeoutSeconds { get; set; } = 30;
    public int PageTimeoutSeconds { get; set; } = 60;
}
