using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;
using StudioSpace.Cli.Models;
using StudioSpace.Cli.Options;

namespace StudioSpace.Cli.Services;

public sealed class PlaywrightSearchService : ISearchService
{
    private readonly ILogger<PlaywrightSearchService> _logger;
    private readonly SearchOptions _options;

    public PlaywrightSearchService(ILogger<PlaywrightSearchService> logger, IOptions<SearchOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<List<StudioListing>> SearchListingsAsync(string postalCode, CancellationToken ct = default)
    {
        var allListings = new List<StudioListing>();
        var debugDir = Path.Combine(_options.ArtifactsPath, "debug");
        Directory.CreateDirectory(debugDir);

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        context.SetDefaultTimeout(_options.PageTimeoutSeconds * 1000);

        _logger.LogInformation("Starting search for photography studio space near {PostalCode}", postalCode);

        // Search sources sequentially
        var searches = new (string Name, Func<IBrowserContext, string, string, CancellationToken, Task<List<StudioListing>>> Search)[]
        {
            ("Kijiji", SearchKijiji),
            ("Spacelist", SearchSpacelist),
            ("Craigslist", SearchCraigslist),
        };

        foreach (var (name, search) in searches)
        {
            try
            {
                var results = await search(context, postalCode, debugDir, ct);
                _logger.LogInformation("{Source}: found {Count} listings", name, results.Count);
                allListings.AddRange(results);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Source} search failed, continuing with others", name);
            }
        }

        _logger.LogInformation("Found {Count} total listings across all sources", allListings.Count);
        return allListings;
    }

    private async Task<List<StudioListing>> SearchKijiji(IBrowserContext context, string postalCode, string debugDir, CancellationToken ct)
    {
        _logger.LogInformation("Searching Kijiji...");
        var listings = new List<StudioListing>();
        var page = await context.NewPageAsync();

        try
        {
            // c40 = Commercial & Office Space category
            // Search photography-specific terms plus general commercial/warehouse/studio in major GTA regions
            var urls = new[]
            {
                // Toronto – photography-specific
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/photography+studio/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/photo+studio/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/photography/k0c40l1700273",
                // Toronto – broader studio / creative / space terms
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/creative+studio/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/studio+space/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/studio+rental/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/studio+for+rent/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/warehouse/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/loft/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/industrial+unit/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/production+space/k0c40l1700273",
                "https://www.kijiji.ca/b-commercial-office-space/city-of-toronto/commercial+unit/k0c40l1700273",
                // Mississauga / Peel
                "https://www.kijiji.ca/b-commercial-office-space/mississauga-peel-region/photography+studio/k0c40l1700276",
                "https://www.kijiji.ca/b-commercial-office-space/mississauga-peel-region/studio+space/k0c40l1700276",
                "https://www.kijiji.ca/b-commercial-office-space/mississauga-peel-region/warehouse/k0c40l1700276",
                "https://www.kijiji.ca/b-commercial-office-space/mississauga-peel-region/industrial+unit/k0c40l1700276",
                "https://www.kijiji.ca/b-commercial-office-space/mississauga-peel-region/commercial+unit/k0c40l1700276",
                // York Region (Markham, Vaughan, Richmond Hill)
                "https://www.kijiji.ca/b-commercial-office-space/markham-york-region/photography+studio/k0c40l1700274",
                "https://www.kijiji.ca/b-commercial-office-space/markham-york-region/studio+space/k0c40l1700274",
                "https://www.kijiji.ca/b-commercial-office-space/markham-york-region/warehouse/k0c40l1700274",
                "https://www.kijiji.ca/b-commercial-office-space/markham-york-region/industrial+unit/k0c40l1700274",
                "https://www.kijiji.ca/b-commercial-office-space/markham-york-region/commercial+unit/k0c40l1700274",
                // Halton (Oakville, Burlington)
                "https://www.kijiji.ca/b-commercial-office-space/oakville-halton-region/studio+space/k0c40l1700277",
                "https://www.kijiji.ca/b-commercial-office-space/oakville-halton-region/warehouse/k0c40l1700277",
                "https://www.kijiji.ca/b-commercial-office-space/oakville-halton-region/commercial+unit/k0c40l1700277",
                // Hamilton
                "https://www.kijiji.ca/b-commercial-office-space/hamilton/studio+space/k0c40l1700242",
                "https://www.kijiji.ca/b-commercial-office-space/hamilton/warehouse/k0c40l1700242",
                "https://www.kijiji.ca/b-commercial-office-space/hamilton/industrial+unit/k0c40l1700242",
                "https://www.kijiji.ca/b-commercial-office-space/hamilton/commercial+unit/k0c40l1700242",
                // Durham (Oshawa, Whitby, Ajax)
                "https://www.kijiji.ca/b-commercial-office-space/oshawa-durham-region/studio+space/k0c40l1700275",
                "https://www.kijiji.ca/b-commercial-office-space/oshawa-durham-region/warehouse/k0c40l1700275",
                "https://www.kijiji.ca/b-commercial-office-space/oshawa-durham-region/commercial+unit/k0c40l1700275",
                // Kitchener-Waterloo
                "https://www.kijiji.ca/b-commercial-office-space/kitchener-waterloo/studio+space/k0c40l1700212",
                "https://www.kijiji.ca/b-commercial-office-space/kitchener-waterloo/warehouse/k0c40l1700212",
                // Ontario-wide catch-all
                "https://www.kijiji.ca/b-commercial-office-space/ontario/photography+studio/k0c40l9004",
                "https://www.kijiji.ca/b-commercial-office-space/ontario/photo+studio/k0c40l9004",
                "https://www.kijiji.ca/b-commercial-office-space/ontario/creative+studio/k0c40l9004",
                "https://www.kijiji.ca/b-commercial-office-space/ontario/warehouse+space/k0c40l9004",
                "https://www.kijiji.ca/b-commercial-office-space/ontario/industrial+unit/k0c40l9004",
            };

            foreach (var url in urls)
            {
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
                await page.WaitForTimeoutAsync(3000);

                await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    Path = Path.Combine(debugDir, $"kijiji_{listings.Count}.png"),
                    FullPage = true
                });

                // Use JsonElement to avoid Playwright's strict type deserialization issues
                var json = await page.EvaluateAsync<JsonElement>(@"() => {
                    const results = [];
                    const cards = document.querySelectorAll('[data-testid=""listing-card""], [data-listing-id], li.regular-ad');
                    for (const card of cards) {
                        try {
                            const link = card.querySelector('a[href*=""/v-""]') || card.querySelector('a[href]');
                            const titleEl = card.querySelector('h3, [data-testid=""listing-title""], a[class*=""title""]');
                            const priceEl = card.querySelector('[data-testid=""listing-price""], p[class*=""price""], span[class*=""price""]');
                            const locEl = card.querySelector('[data-testid=""listing-location""], span[class*=""location""], p[class*=""location""]');
                            const imgEl = card.querySelector('img[src*=""http""]');
                            const href = link ? (link.getAttribute('href') || '') : '';
                            const title = titleEl ? (titleEl.innerText || '') : '';
                            const price = priceEl ? (priceEl.innerText || '') : '';
                            const location = locEl ? (locEl.innerText || '') : '';
                            const imageUrl = imgEl ? (imgEl.getAttribute('src') || '') : '';
                            if (title.length === 0 && price.length === 0) continue;
                            results.push({
                                u: href.startsWith('http') ? href : ('https://www.kijiji.ca' + href),
                                t: title.trim(),
                                p: price.trim(),
                                l: location.trim(),
                                i: imageUrl
                            });
                        } catch(e) {}
                    }
                    return results;
                }");

                var results = ParseJsonArray(json);
                _logger.LogInformation("Kijiji URL returned {Count} card results", results.Count);

                foreach (var r in results.Take(_options.MaxListings))
                {
                    var title = r.GetValueOrDefault("t", "");
                    var urlStr = r.GetValueOrDefault("u", "");
                    if (string.IsNullOrWhiteSpace(title) || listings.Any(l => l.ListingUrl == urlStr)) continue;

                    // Filter: only commercial spaces suitable for photography
                    if (IsResidentialListing(title, urlStr))
                    {
                        _logger.LogDebug("Skipping residential listing: {Title}", title);
                        continue;
                    }

                    if (!IsPhotographySuitable(title))
                    {
                        _logger.LogDebug("Skipping non-photography listing: {Title}", title);
                        continue;
                    }

                    var price = r.GetValueOrDefault("p", "");
                    var listing = new StudioListing
                    {
                        Description = title,
                        ListingUrl = urlStr,
                        Address = r.GetValueOrDefault("l", "Mississauga / Peel Region, ON"),
                        Source = "Kijiji",
                        RentalCost = !string.IsNullOrWhiteSpace(price) ? price : null,
                    };

                    var imgUrl = r.GetValueOrDefault("i", "");
                    if (imgUrl.StartsWith("http"))
                        listing.ImageUrls.Add(imgUrl);

                    listings.Add(listing);
                }
            }

            // Scrape detail pages for top listings to get more images
            foreach (var listing in listings.Take(8))
            {
                try
                {
                    await ScrapeDetailPage(context, listing, debugDir, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not scrape Kijiji detail for {Url}", listing.ListingUrl);
                }
            }

            return listings;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<List<StudioListing>> SearchSpacelist(IBrowserContext context, string postalCode, string debugDir, CancellationToken ct)
    {
        _logger.LogInformation("Searching Spacelist.ca...");
        var listings = new List<StudioListing>();
        var page = await context.NewPageAsync();

        try
        {
            // Spacelist.ca is a Canadian commercial real estate search engine – photography focused
            var urls = new[]
            {
                "https://www.spacelist.ca/search?type=lease&location=Toronto+ON&keywords=photography+studio",
                "https://www.spacelist.ca/search?type=sale&location=Toronto+ON&keywords=photography+studio",
                "https://www.spacelist.ca/search?type=lease&location=Toronto+ON&keywords=studio",
                "https://www.spacelist.ca/search?type=lease&location=Mississauga+ON&keywords=photography+studio",
                "https://www.spacelist.ca/search?type=sale&location=Mississauga+ON&keywords=photography+studio",
                "https://www.spacelist.ca/search?type=lease&location=Mississauga+ON&keywords=studio",
                "https://www.spacelist.ca/search?type=lease&location=Hamilton+ON&keywords=studio",
                "https://www.spacelist.ca/search?type=lease&location=Markham+ON&keywords=studio",
                "https://www.spacelist.ca/search?type=lease&location=Vaughan+ON&keywords=studio",
                "https://www.spacelist.ca/search?type=lease&location=Brampton+ON&keywords=studio",
                "https://www.spacelist.ca/search?type=lease&location=Oakville+ON&keywords=studio",
                "https://www.spacelist.ca/search?type=lease&location=Kitchener+ON&keywords=studio",
            };

            int urlIdx = 0;
            foreach (var url in urls)
            {
                urlIdx++;
                try
                {
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 20000
                    });
                    await page.WaitForTimeoutAsync(3000);

                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(debugDir, $"spacelist_{urlIdx}.png"),
                        FullPage = true
                    });

                    var json = await page.EvaluateAsync<JsonElement>(@"() => {
                        const results = [];
                        const cards = document.querySelectorAll('[class*=""listing""], [class*=""property""], [class*=""card""], article, .search-result');
                        for (const card of cards) {
                            try {
                                const link = card.querySelector('a[href*=""/l/""], a[href*=""/listing""], a[href]');
                                const title = card.querySelector('h2, h3, [class*=""title""], [class*=""name""]');
                                const price = card.querySelector('[class*=""price""], [class*=""rate""], [class*=""Price""]');
                                const addr = card.querySelector('[class*=""address""], [class*=""location""], [class*=""Address""]');
                                const img = card.querySelector('img[src*=""http""]');
                                if (!link && !title) continue;
                                const href = link ? (link.getAttribute('href') || '') : '';
                                results.push({
                                    u: href.startsWith('http') ? href : ('https://www.spacelist.ca' + href),
                                    t: (title ? title.innerText : '').trim(),
                                    p: (price ? price.innerText : '').trim(),
                                    a: (addr ? addr.innerText : '').trim(),
                                    i: img ? (img.getAttribute('src') || '') : ''
                                });
                            } catch(e) {}
                        }
                        return results;
                    }");

                    var results = ParseJsonArray(json);
                    var transactionType = url.Contains("type=sale") ? "sale" : "lease";
                    _logger.LogInformation("Spacelist ({Type}) returned {Count} results", transactionType, results.Count);

                    foreach (var r in results.Where(r => !string.IsNullOrWhiteSpace(r.GetValueOrDefault("t", ""))).Take(_options.MaxListings))
                    {
                        var urlStr = r.GetValueOrDefault("u", "");
                        if (listings.Any(l => l.ListingUrl == urlStr)) continue;

                        var title = r.GetValueOrDefault("t", "");
                        if (!IsPhotographySuitable(title))
                        {
                            _logger.LogDebug("Skipping non-photography Spacelist listing: {Title}", title);
                            continue;
                        }

                        var price = r.GetValueOrDefault("p", "");
                        var listing = new StudioListing
                        {
                            Description = r.GetValueOrDefault("t", ""),
                            ListingUrl = urlStr,
                            Address = r.GetValueOrDefault("a", $"Near {postalCode}"),
                            Source = "Spacelist.ca",
                        };

                        if (!string.IsNullOrWhiteSpace(price))
                        {
                            if (transactionType == "lease")
                                listing.RentalCost = price;
                            else
                                listing.PurchaseCost = price;
                        }

                        var imgUrl = r.GetValueOrDefault("i", "");
                        if (imgUrl.StartsWith("http"))
                            listing.ImageUrls.Add(imgUrl);

                        listings.Add(listing);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Spacelist URL {Url} failed", url);
                }
            }

            // Scrape detail pages
            foreach (var listing in listings.Take(6))
            {
                try
                {
                    await ScrapeDetailPage(context, listing, debugDir, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not scrape Spacelist detail for {Url}", listing.ListingUrl);
                }
            }

            return listings;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task<List<StudioListing>> SearchCraigslist(IBrowserContext context, string postalCode, string debugDir, CancellationToken ct)
    {
        _logger.LogInformation("Searching Craigslist Toronto...");
        var listings = new List<StudioListing>();
        var page = await context.NewPageAsync();

        try
        {
            // off = offices & commercial, search both Toronto and Hamilton Craigslist
            var urls = new[]
            {
                "https://toronto.craigslist.org/search/off?query=photography+studio",
                "https://toronto.craigslist.org/search/off?query=photo+studio",
                "https://toronto.craigslist.org/search/off?query=creative+studio",
                "https://toronto.craigslist.org/search/off?query=studio+space",
                "https://toronto.craigslist.org/search/off?query=warehouse+space",
                "https://toronto.craigslist.org/search/off?query=industrial+unit",
                "https://toronto.craigslist.org/search/off?query=loft+space",
                "https://toronto.craigslist.org/search/off?query=commercial+space",
                "https://hamilton.craigslist.org/search/off?query=studio+space",
                "https://hamilton.craigslist.org/search/off?query=warehouse+space",
                "https://hamilton.craigslist.org/search/off?query=commercial+space",
            };

            int urlIdx = 0;
            foreach (var url in urls)
            {
                urlIdx++;
                try
                {
                    await page.GotoAsync(url, new PageGotoOptions
                    {
                        WaitUntil = WaitUntilState.DOMContentLoaded,
                        Timeout = 20000
                    });
                    await page.WaitForTimeoutAsync(2000);

                    await page.ScreenshotAsync(new PageScreenshotOptions
                    {
                        Path = Path.Combine(debugDir, $"craigslist_{urlIdx}.png"),
                        FullPage = true
                    });

                    var json = await page.EvaluateAsync<JsonElement>(@"() => {
                        const results = [];
                        const rows = document.querySelectorAll('.cl-search-result, li.result-row, .result-info');
                        for (const row of rows) {
                            try {
                                const link = row.querySelector('a[href*=""craigslist""], a.posting-title, a.result-title, a[href]');
                                const titleEl = row.querySelector('.posting-title .label, .result-title, a .label, .title');
                                const priceEl = row.querySelector('.priceinfo, .result-price, .price');
                                const locEl = row.querySelector('.meta .subreddit, .result-hood, .nearby');
                                const imgEl = row.querySelector('img[src*=""http""]');
                                if (!link && !titleEl) continue;
                                const href = link ? (link.getAttribute('href') || '') : '';
                                const title = titleEl ? (titleEl.innerText || titleEl.textContent || '') : (link ? link.innerText : '');
                                results.push({
                                    u: href.startsWith('http') ? href : ('https://toronto.craigslist.org' + href),
                                    t: title.trim(),
                                    p: priceEl ? priceEl.innerText.trim() : '',
                                    l: locEl ? locEl.innerText.trim() : 'GTA, Ontario',
                                    i: imgEl ? (imgEl.getAttribute('src') || '') : ''
                                });
                            } catch(e) {}
                        }
                        return results;
                    }");

                    var results = ParseJsonArray(json);
                    _logger.LogInformation("Craigslist URL returned {Count} results", results.Count);

                    foreach (var r in results.Take(_options.MaxListings))
                    {
                        var title = r.GetValueOrDefault("t", "");
                        var urlStr = r.GetValueOrDefault("u", "");
                        if (string.IsNullOrWhiteSpace(title) || listings.Any(l => l.ListingUrl == urlStr)) continue;

                        if (IsResidentialListing(title, urlStr)) continue;
                        if (!IsPhotographySuitable(title)) continue;

                        var price = r.GetValueOrDefault("p", "");
                        var listing = new StudioListing
                        {
                            Description = title,
                            ListingUrl = urlStr,
                            Address = r.GetValueOrDefault("l", "GTA, Ontario"),
                            Source = "Craigslist",
                            RentalCost = !string.IsNullOrWhiteSpace(price) ? price : null,
                        };

                        var imgUrl = r.GetValueOrDefault("i", "");
                        if (imgUrl.StartsWith("http"))
                            listing.ImageUrls.Add(imgUrl);

                        listings.Add(listing);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Craigslist URL {Url} failed", url);
                }
            }

            // Scrape detail pages
            foreach (var listing in listings.Take(8))
            {
                try
                {
                    await ScrapeDetailPage(context, listing, debugDir, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not scrape Craigslist detail for {Url}", listing.ListingUrl);
                }
            }

            return listings;
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task ScrapeDetailPage(IBrowserContext context, StudioListing listing, string debugDir, CancellationToken ct)
    {
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(listing.ListingUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 15000
            });
            await page.WaitForTimeoutAsync(2000);

            var json = await page.EvaluateAsync<JsonElement>(@"() => {
                const images = [];
                const allImgs = document.querySelectorAll('img[src*=""http""]');
                for (const img of allImgs) {
                    const src = img.getAttribute('src') || '';
                    const w = img.naturalWidth || img.width || 0;
                    const h = img.naturalHeight || img.height || 0;
                    if (w > 80 && h > 80 && src.startsWith('http')) {
                        const lower = src.toLowerCase();
                        if (!lower.includes('logo') && !lower.includes('icon') && !lower.includes('favicon')
                            && !lower.includes('pixel') && !lower.includes('tracking')
                            && !lower.includes('google') && !lower.includes('facebook')
                            && !lower.includes('analytics') && !lower.includes('badge')) {
                            images.push(src);
                        }
                    }
                }
                const galleries = document.querySelectorAll('[class*=""gallery""] img, [class*=""photo""] img, [class*=""image""] img, [class*=""slider""] img, [class*=""carousel""] img, [class*=""hero""] img');
                for (const img of galleries) {
                    const src = img.getAttribute('src') || img.getAttribute('data-src') || '';
                    if (src.startsWith('http') && !images.includes(src)) images.push(src);
                }
                const bodyText = document.body ? document.body.innerText.substring(0, 5000) : '';
                const addrEl = document.querySelector('[class*=""address""], [itemprop=""address""], [data-testid*=""address""], address, [class*=""Address""]');
                const address = addrEl ? (addrEl.innerText || '').trim() : '';
                return { images: images.slice(0, 8), bodyText: bodyText, address: address };
            }");

            if (json.ValueKind == JsonValueKind.Object)
            {
                if (json.TryGetProperty("images", out var imgArr) && imgArr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in imgArr.EnumerateArray())
                    {
                        var url = img.GetString() ?? "";
                        if (url.StartsWith("http") && !listing.ImageUrls.Contains(url))
                            listing.ImageUrls.Add(url);
                    }
                }

                var bodyText = json.TryGetProperty("bodyText", out var bt) ? bt.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(listing.RentalCost))
                    listing.RentalCost = ExtractPrice(bodyText, "rent");
                if (string.IsNullOrWhiteSpace(listing.PurchaseCost))
                    listing.PurchaseCost = ExtractPrice(bodyText, "sale");

                var addr = json.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "";
                if (listing.Address.Contains("Near") && !string.IsNullOrWhiteSpace(addr))
                    listing.Address = addr.Length > 150 ? addr[..150] : addr;
            }
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// Parse the JsonElement array returned from EvaluateAsync into a list of string dictionaries.
    private static List<Dictionary<string, string>> ParseJsonArray(JsonElement json)
    {
        var results = new List<Dictionary<string, string>>();
        if (json.ValueKind != JsonValueKind.Array) return results;

        foreach (var item in json.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object) continue;
            var dict = new Dictionary<string, string>();
            foreach (var prop in item.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                    ? prop.Value.GetString() ?? ""
                    : prop.Value.ToString();
            }
            results.Add(dict);
        }

        return results;
    }

    /// Returns true if the listing appears to be residential rather than commercial.
    private static bool IsResidentialListing(string title, string url)
    {
        var lower = title.ToLowerInvariant();
        var lowerUrl = url.ToLowerInvariant();

        // Reject if the Kijiji URL path belongs to a residential category
        string[] residentialUrlPaths =
        [
            "/v-apartments-condos/",
            "/v-house-rental/",
            "/v-room-rental/",
            "/v-short-term-rental/",
            "/v-condos-for-sale/",
            "/v-houses-for-sale/",
        ];

        if (residentialUrlPaths.Any(p => lowerUrl.Contains(p)))
            return true;

        // Reject if title contains residential keywords
        string[] residentialKeywords =
        [
            "bedroom", "bachelor", "apartment", "condo", "townhouse",
            "house for rent", "home for rent", "homes for rent",
            "basement", "furnished room", "roommate",
            "1 bed", "2 bed", "3 bed", "4 bed", "5 bed",
            "1bed", "2bed", "3bed",
            "bdrm", "ensuite",
        ];

        return residentialKeywords.Any(k => lower.Contains(k));
    }

    /// Returns true if the listing appears suitable for use as a photography studio.
    /// Uses a blocklist approach: reject clearly unsuitable business types,
    /// accept everything else from the commercial category since open commercial
    /// spaces (warehouses, industrial units, lofts, etc.) can serve as photography studios.
    private static bool IsPhotographySuitable(string title)
    {
        var lower = title.ToLowerInvariant();

        // Reject very short or meaningless titles
        if (lower.Trim().Length < 8)
            return false;

        // Reject listings that are clearly unsuitable for photography studio use
        string[] unsuitableKeywords =
        [
            // Personal care / beauty
            "hair salon", "hair studio", "beauty salon", "nail salon", "nail studio",
            "barbershop", "barber shop", "barber studio",
            "massage", "spa ", "esthetics", "aesthetics", "skincare",
            "tattoo", "piercing",
            "beauty treatment", "treatment room", "beauty studio",
            "nails/", "/nails", "nails ", "groomer", "grooming",
            "chair rental", "chair & room",
            // Activity-specific studios
            "yoga", "pilates", "dance studio", "dance school", "dance class",
            "martial art", "boxing gym", "crossfit",
            "acting class", "acting school", "voice over", "voiceover",
            "music studio", "recording studio", "podcast studio",
            "rehearsal space", "rehearsal room", "music rehearsal",
            // Medical / professional
            "dental", "dentist", "medical office", "clinic", "pharmacy", "optometrist",
            "chiropract", "physiotherapy", "veterinar",
            "law office", "law firm", "accounting", "tax office",
            // Food & hospitality
            "restaurant", "food truck", "bakery", "cafe ", "café", "coffee shop",
            "food court", "food hall", "kitchen for rent", "catering",
            "food & beverage", "food and beverage", "food processing",
            "bar ", "pub ", "lounge", "nightclub",
            "walkin cooler", "walk-in cooler", "walk in cooler", "walkin freezer", "walk-in freezer",
            "cold storage",
            // Fitness
            "gym ", "fitness studio", "fitness centre", "fitness center",
            // Childcare
            "daycare", "child care", "childcare", "preschool", "montessori",
            // Automotive
            "auto shop", "mechanic", "car wash", "auto body", "tire shop",
            "car dealer", "gas station", "dealer licence", "dealer license",
            // Cannabis
            "dispensary", "cannabis",
            // Event-specific
            "event space", "event venue", "banquet hall", "wedding venue", "wedding hall",
            "party room", "party hall", "venue for rent",
            // Specific industrial use
            "metalworking", "welding", "machine shop", "woodworking",
            // Storage / parking
            "parking", "storage unit", "self storage", "mini storage",
            "locker",
            // Not a space (services, ads, etc.)
            "virtual office",
            "cabin ", "home office", "shed ",
            "deals on now", "call today",
            // Residential indicators
            "for sale by owner", "open house",
            "work&live", "live/work", "work/live", "live work",
            // Generic shared office
            "shared office", "co-working", "coworking",
            // Clearly small/wrong format
            "desk space", "hot desk", "cubicle",
        ];

        if (unsuitableKeywords.Any(k => lower.Contains(k)))
            return false;

        // Reject pure office listings (no studio/warehouse/creative/photo context)
        if (lower.Contains("office") && !lower.Contains("studio") && !lower.Contains("warehouse")
            && !lower.Contains("creative") && !lower.Contains("photo") && !lower.Contains("production"))
            return false;

        return true;
    }

    private static string ExtractAddressFromText(string? text, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(text))
            return $"Near {postalCode}";

        var lines = text.Split(['\n', '\r', '·', '|'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (System.Text.RegularExpressions.Regex.IsMatch(trimmed,
                @"\d+\s+\w+\s+(St|Ave|Rd|Dr|Blvd|Cres|Way|Ct|Ln|Pl|Street|Avenue|Road|Drive|Boulevard)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                return trimmed.Length > 120 ? trimmed[..120] : trimmed;
        }

        return $"Near {postalCode}";
    }

    private static string? ExtractPrice(string? text, string type)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var patterns = type == "rent"
            ? new[]
            {
                @"\$[\d,]+(?:\.\d{2})?\s*/\s*(?:mo(?:nth)?|yr|year)",
                @"\$[\d,]+(?:\.\d{2})?\s*/\s*(?:sf|sqft|sq\.?\s*ft)",
                @"\$[\d,]+(?:\.\d{2})?\s*(?:per\s+(?:month|year|sf|sqft))",
                @"(?:rent|lease)[:\s]*\$[\d,]+(?:\.\d{2})?",
                @"\$[\d,]+(?:\.\d{2})?\s*/mo",
            }
            : new[]
            {
                @"\$[\d,]+(?:\.\d{2})?\s*(?:asking|sale|purchase)",
                @"(?:price|asking|sale)[:\s]*\$[\d,]+(?:\.\d{2})?",
                @"\$\d{1,3}(?:,\d{3}){2,}",  // prices like $500,000 or $1,200,000
            };

        foreach (var pattern in patterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Value.Trim();
        }

        return null;
    }
}
