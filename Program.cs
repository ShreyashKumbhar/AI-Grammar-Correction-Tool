using GrammarCorrector.Services;
using Microsoft.Extensions.FileProviders;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();

// Register HttpClient for LanguageTool API calls
builder.Services.AddHttpClient<IGrammarService, GrammarService>(client =>
{
    client.BaseAddress = new Uri("https://api.languagetool.org");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure static files to serve from the project root instead of wwwroot
var fileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
    app.Environment.ContentRootPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = ""
});

app.UseCors();
app.MapControllers();

// Serve index.html for any unmatched route (SPA fallback)
app.MapFallbackToFile("index.html", new StaticFileOptions
{
    FileProvider = fileProvider,
    RequestPath = ""
});

app.Run();
