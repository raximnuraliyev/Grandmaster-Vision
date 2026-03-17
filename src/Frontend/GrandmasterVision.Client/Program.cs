using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GrandmasterVision.Client;
using GrandmasterVision.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure API client - use same origin (nginx proxies /api to backend)
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});

// Register services
builder.Services.AddScoped<ChessApiService>();

// Lichess Explorer Service (external API)
builder.Services.AddScoped<LichessExplorerService>(sp =>
{
    var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    var logger = sp.GetRequiredService<ILogger<LichessExplorerService>>();
    return new LichessExplorerService(httpClient, logger);
});

// Add logging
builder.Logging.SetMinimumLevel(LogLevel.Warning);

await builder.Build().RunAsync();
