using GrandmasterVision.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Add CORS for Blazor frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazor", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Register chess services
var stockfishPath = Environment.GetEnvironmentVariable("STOCKFISH_PATH")
    ?? (OperatingSystem.IsLinux() ? "/usr/games/stockfish" : Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "..", "..", "..", "..", "engine", "stockfish", "stockfish-windows-x86-64-avx2.exe"));

builder.Services.AddSingleton(sp => new StockfishService(stockfishPath));
builder.Services.AddSingleton<PgnAnalyzerService>();
builder.Services.AddHttpClient<OpeningService>();

// Configure HttpClient for Vision service
var visionUrl = builder.Configuration["VisionService:BaseUrl"] ?? "http://localhost:8000";
builder.Services.AddHttpClient("VisionService", client =>
{
    client.BaseAddress = new Uri(visionUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazor");
app.MapControllers();

// Initialize Stockfish on startup
var stockfish = app.Services.GetRequiredService<StockfishService>();
await stockfish.InitializeAsync();

app.Run();
