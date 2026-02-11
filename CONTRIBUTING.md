# Contributing

Thanks for your interest in contributing to StudioSpace! This guide covers how to set up the project, make changes, and submit them.

## Development Setup

1. Install the [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
2. Clone the repo and restore dependencies:

```bash
git clone <repo-url>
cd StudioSpace
dotnet restore src/StudioSpace.Cli/StudioSpace.Cli.csproj
```

3. Install Playwright browsers:

```bash
dotnet build src/StudioSpace.Cli/StudioSpace.Cli.csproj
pwsh src/StudioSpace.Cli/bin/Debug/net9.0/playwright.ps1 install chromium
```

4. Run the application:

```bash
dotnet run --project src/StudioSpace.Cli -- search
```

## Project Layout

All source code lives under `src/StudioSpace.Cli/`:

- **Commands/** — CLI command handlers
- **Models/** — Data models (e.g. `StudioListing`)
- **Options/** — Configuration/options classes
- **Services/** — Core logic: scraping, report generation, image downloading
- **Program.cs** — Entry point, DI registration, and host configuration

## Making Changes

### Branching

1. Create a branch off `main`:

```bash
git checkout -b your-feature-name
```

2. Make your changes in small, focused commits.
3. Push your branch and open a pull request against `main`.

### Code Style

- Follow standard C# conventions and the existing patterns in the codebase.
- Use dependency injection — register new services in `Program.cs`.
- All services should implement an interface (see `ISearchService`, `IReportGenerator`, etc.).
- Use `async/await` for any I/O-bound operations.
- Keep nullable reference types enabled (`<Nullable>enable</Nullable>`).

### Adding a New Listing Source

1. Create a new class implementing `ISearchService` in `Services/`.
2. Register it in `Program.cs` via the DI container.
3. Follow the patterns in `PlaywrightSearchService.cs` for browser automation, error handling, and logging.
4. Add any new search URLs to `appsettings.json` if needed.

### Adding New Report Formats

1. Create a new class implementing `IReportGenerator` in `Services/`.
2. Register and invoke it alongside the existing generators.

## Testing

There is no test suite yet. If you add tests:

- Place test projects under a `tests/` directory at the repo root.
- Use **xUnit** as the test framework to match .NET ecosystem conventions.
- Name test projects `StudioSpace.<Area>.Tests` (e.g. `StudioSpace.Cli.Tests`).

## Reporting Issues

Open an issue on the repository with:

- A clear description of the problem or feature request.
- Steps to reproduce (if it's a bug).
- Expected vs. actual behavior.

## Pull Request Guidelines

- Keep PRs focused on a single change.
- Write a clear title and description explaining *what* changed and *why*.
- Ensure the project builds without warnings: `dotnet build src/StudioSpace.Cli/StudioSpace.Cli.csproj`.
- Test your changes by running a search and verifying the output reports.
