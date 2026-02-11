using System.CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudioSpace.Cli.Commands;
using StudioSpace.Cli.Options;
using StudioSpace.Cli.Services;

// --- Configuration ---
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables("STUDIO_")
    .Build();

// --- DI Container ---
var services = new ServiceCollection();

// Options
services.Configure<SearchOptions>(configuration.GetSection(SearchOptions.SectionName));

// Logging
services.AddLogging(builder =>
{
    builder.AddConfiguration(configuration.GetSection("Logging"));
    builder.AddConsole();
});

// Services
services.AddSingleton<ISearchService, PlaywrightSearchService>();
services.AddSingleton<IReportGenerator, ReportGenerator>();
services.AddHttpClient<IImageDownloader, ImageDownloader>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
});

var serviceProvider = services.BuildServiceProvider();

// --- CLI ---
var rootCommand = new RootCommand("StudioSpace.Cli - Find photography studio space for rent or sale");

var searchCommand = new SearchStudiosCommand();
searchCommand.Action = new SearchStudiosHandler(serviceProvider);
rootCommand.Subcommands.Add(searchCommand);

// --- Parse & Invoke ---
return await rootCommand.Parse(args).InvokeAsync();
