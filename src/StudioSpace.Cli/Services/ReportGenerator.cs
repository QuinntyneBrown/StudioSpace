using System.Text;
using Microsoft.Extensions.Logging;
using StudioSpace.Cli.Models;

namespace StudioSpace.Cli.Services;

public sealed class ReportGenerator : IReportGenerator
{
    private readonly ILogger<ReportGenerator> _logger;

    public ReportGenerator(ILogger<ReportGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateReportAsync(List<StudioListing> listings, string outputDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);

        var mdPath = Path.Combine(outputDir, "studio_space_report.md");
        var htmlPath = Path.Combine(outputDir, "studio_space_report.html");

        var md = BuildMarkdown(listings);
        var html = BuildHtml(listings);

        await File.WriteAllTextAsync(mdPath, md, ct);
        await File.WriteAllTextAsync(htmlPath, html, ct);

        _logger.LogInformation("Report generated: {MdPath}", mdPath);
        _logger.LogInformation("Report generated: {HtmlPath}", htmlPath);

        return mdPath;
    }

    private static string BuildMarkdown(List<StudioListing> listings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Photography Studio Space Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"**Search Area:** Near postal code L5A 4E6 (Mississauga, Ontario)");
        sb.AppendLine($"**Total Listings Found:** {listings.Count}");
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Summary table
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine("| # | Address | Rental Cost | Purchase Cost | Source |");
        sb.AppendLine("|---|---------|-------------|---------------|--------|");

        int idx = 0;
        foreach (var l in listings)
        {
            idx++;
            sb.AppendLine($"| {idx} | {Escape(l.Address)} | {Escape(l.RentalCost ?? "N/A")} | {Escape(l.PurchaseCost ?? "N/A")} | {Escape(l.Source)} |");
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();

        // Detailed listings
        sb.AppendLine("## Detailed Listings");
        sb.AppendLine();

        idx = 0;
        foreach (var l in listings)
        {
            idx++;
            sb.AppendLine($"### {idx}. {l.Description}");
            sb.AppendLine();
            sb.AppendLine($"- **Address:** {l.Address}");
            sb.AppendLine($"- **Rental Cost:** {l.RentalCost ?? "N/A"}");
            sb.AppendLine($"- **Purchase Cost:** {l.PurchaseCost ?? "N/A"}");
            sb.AppendLine($"- **Source:** {l.Source}");
            sb.AppendLine($"- **Listing URL:** [{l.ListingUrl}]({l.ListingUrl})");
            sb.AppendLine();

            if (l.LocalImagePaths.Count > 0)
            {
                sb.AppendLine("**Images:**");
                sb.AppendLine();
                foreach (var img in l.LocalImagePaths)
                {
                    sb.AppendLine($"![Listing Image]({img})");
                    sb.AppendLine();
                }
            }

            sb.AppendLine("---");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildHtml(List<StudioListing> listings)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"UTF-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("  <title>Photography Studio Space Report</title>");
        sb.AppendLine("  <style>");
        sb.AppendLine("    * { box-sizing: border-box; margin: 0; padding: 0; }");
        sb.AppendLine("    body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #f5f5f5; color: #333; padding: 2rem; }");
        sb.AppendLine("    .container { max-width: 1200px; margin: 0 auto; }");
        sb.AppendLine("    h1 { font-size: 2rem; margin-bottom: 0.5rem; color: #1a1a2e; }");
        sb.AppendLine("    .meta { color: #666; margin-bottom: 2rem; }");
        sb.AppendLine("    .listing { background: #fff; border-radius: 12px; padding: 1.5rem; margin-bottom: 1.5rem; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }");
        sb.AppendLine("    .listing h2 { font-size: 1.25rem; color: #1a1a2e; margin-bottom: 0.75rem; }");
        sb.AppendLine("    .listing .details { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 0.75rem; margin-bottom: 1rem; }");
        sb.AppendLine("    .listing .detail-item { padding: 0.5rem; background: #f8f9fa; border-radius: 6px; }");
        sb.AppendLine("    .listing .detail-item label { font-size: 0.75rem; text-transform: uppercase; color: #888; display: block; }");
        sb.AppendLine("    .listing .detail-item span { font-weight: 600; }");
        sb.AppendLine("    .listing .images { display: flex; gap: 0.75rem; flex-wrap: wrap; }");
        sb.AppendLine("    .listing .images img { width: 200px; height: 150px; object-fit: cover; border-radius: 8px; border: 1px solid #eee; }");
        sb.AppendLine("    .listing a { color: #2563eb; text-decoration: none; }");
        sb.AppendLine("    .listing a:hover { text-decoration: underline; }");
        sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-bottom: 2rem; background: #fff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08); }");
        sb.AppendLine("    th { background: #1a1a2e; color: #fff; text-align: left; padding: 0.75rem 1rem; font-size: 0.85rem; text-transform: uppercase; }");
        sb.AppendLine("    td { padding: 0.75rem 1rem; border-bottom: 1px solid #eee; font-size: 0.9rem; }");
        sb.AppendLine("    tr:last-child td { border-bottom: none; }");
        sb.AppendLine("  </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("  <div class=\"container\">");
        sb.AppendLine($"    <h1>Photography Studio Space Report</h1>");
        sb.AppendLine($"    <p class=\"meta\">Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Search Area: Near L5A 4E6 (Mississauga, ON) | Listings: {listings.Count}</p>");

        // Summary table
        sb.AppendLine("    <table>");
        sb.AppendLine("      <thead><tr><th>#</th><th>Address</th><th>Rental Cost</th><th>Purchase Cost</th><th>Source</th><th>Link</th></tr></thead>");
        sb.AppendLine("      <tbody>");
        int idx = 0;
        foreach (var l in listings)
        {
            idx++;
            sb.AppendLine($"        <tr><td>{idx}</td><td>{HtmlEncode(l.Address)}</td><td>{HtmlEncode(l.RentalCost ?? "N/A")}</td><td>{HtmlEncode(l.PurchaseCost ?? "N/A")}</td><td>{HtmlEncode(l.Source)}</td><td><a href=\"{HtmlEncode(l.ListingUrl)}\" target=\"_blank\">View</a></td></tr>");
        }
        sb.AppendLine("      </tbody>");
        sb.AppendLine("    </table>");

        // Detailed cards
        idx = 0;
        foreach (var l in listings)
        {
            idx++;
            sb.AppendLine($"    <div class=\"listing\">");
            sb.AppendLine($"      <h2>{idx}. {HtmlEncode(l.Description)}</h2>");
            sb.AppendLine($"      <div class=\"details\">");
            sb.AppendLine($"        <div class=\"detail-item\"><label>Address</label><span>{HtmlEncode(l.Address)}</span></div>");
            sb.AppendLine($"        <div class=\"detail-item\"><label>Rental Cost</label><span>{HtmlEncode(l.RentalCost ?? "N/A")}</span></div>");
            sb.AppendLine($"        <div class=\"detail-item\"><label>Purchase Cost</label><span>{HtmlEncode(l.PurchaseCost ?? "N/A")}</span></div>");
            sb.AppendLine($"        <div class=\"detail-item\"><label>Source</label><span>{HtmlEncode(l.Source)}</span></div>");
            sb.AppendLine($"        <div class=\"detail-item\"><label>Listing URL</label><a href=\"{HtmlEncode(l.ListingUrl)}\" target=\"_blank\">{HtmlEncode(TruncateUrl(l.ListingUrl))}</a></div>");
            sb.AppendLine($"      </div>");

            if (l.LocalImagePaths.Count > 0)
            {
                sb.AppendLine($"      <div class=\"images\">");
                foreach (var img in l.LocalImagePaths)
                {
                    sb.AppendLine($"        <img src=\"{HtmlEncode(img)}\" alt=\"Listing photo\" loading=\"lazy\">");
                }
                sb.AppendLine($"      </div>");
            }

            sb.AppendLine($"    </div>");
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    private static string Escape(string text) => text.Replace("|", "\\|");
    private static string HtmlEncode(string text) => System.Net.WebUtility.HtmlEncode(text);
    private static string TruncateUrl(string url) => url.Length > 60 ? url[..57] + "..." : url;
}
