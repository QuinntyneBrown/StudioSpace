# StudioSpace

A .NET CLI tool that searches for photography studio rental and sale listings in the Mississauga/Toronto area. It scrapes multiple listing sources using Playwright, downloads images, and generates comprehensive HTML and Markdown reports.

## Features

- Scrapes **Kijiji** and **Spacelist.ca** for commercial studio spaces
- Filters out residential listings automatically
- Downloads listing images and captures debug screenshots
- Generates styled **HTML** and **Markdown** reports with pricing, addresses, and photos
- Configurable search area, output directory, and listing limits

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- Playwright browsers (installed automatically on first run, or manually via the command below)

## Getting Started

```bash
# Clone the repository
git clone <repo-url>
cd StudioSpace

# Restore dependencies
dotnet restore src/StudioSpace.Cli/StudioSpace.Cli.csproj

# Install Playwright browsers
pwsh src/StudioSpace.Cli/bin/Debug/net9.0/playwright.ps1 install chromium
```

## Usage

```bash
dotnet run --project src/StudioSpace.Cli -- search
```

### Options

| Option | Alias | Default | Description |
|---|---|---|---|
| `--postal-code` | `-p` | `L5A 4E6` | Postal code for the search area |
| `--output` | `-o` | `artifacts` | Output directory for reports and images |
| `--max-listings` | `-m` | `20` | Maximum number of listings per source |

### Examples

```bash
# Search with defaults (Mississauga area)
dotnet run --project src/StudioSpace.Cli -- search

# Search a different area with more results
dotnet run --project src/StudioSpace.Cli -- search -p "M5V 2H1" -m 50

# Custom output directory
dotnet run --project src/StudioSpace.Cli -- search -o ./my-reports
```

## Output

Reports are written to the `artifacts/` directory:

```
artifacts/
├── debug/                         # Full-page screenshots for debugging
├── images/                        # Downloaded listing images
├── studio_space_report.html       # Styled HTML report
└── studio_space_report.md         # Markdown report
```

## Project Structure

```
src/StudioSpace.Cli/
├── Commands/
│   └── SearchStudiosCommand.cs    # CLI command definition
├── Models/
│   └── StudioListing.cs           # Listing data model
├── Options/
│   └── SearchOptions.cs           # Configuration options
├── Services/
│   ├── PlaywrightSearchService.cs # Web scraping logic
│   ├── ReportGenerator.cs         # HTML/Markdown report output
│   └── ImageDownloader.cs         # Listing image downloader
├── appsettings.json               # Default configuration
└── Program.cs                     # Entry point and DI setup
```

## Configuration

Default settings live in `src/StudioSpace.Cli/appsettings.json`:

```json
{
  "Search": {
    "PostalCode": "L5A 4E6",
    "ArtifactsPath": "artifacts",
    "MaxListings": 20,
    "ImageDownloadTimeoutSeconds": 30,
    "PageTimeoutSeconds": 60
  }
}
```

## Tech Stack

- **.NET 9.0** with top-level statements
- **Microsoft.Playwright** for browser automation and scraping
- **System.CommandLine** for CLI argument parsing
- **Microsoft.Extensions.Hosting** for dependency injection and configuration

## License

This project is for personal use.
