using Microsoft.Extensions.Configuration;
using sample_scan_passport_using_ai.Services;

Console.WriteLine("=== Identity Document Scan AI ===");
Console.WriteLine("Smart, simple, and ready to process your files.");

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

var apiKey = configuration["Gemini:ApiKey"];
var modelName = configuration["Gemini:ModelName"];
var sourceFolder = configuration["Folder:SourceFolder"];
var destinationFolder = configuration["Folder:DestinationFolder"];

if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(modelName) || string.IsNullOrWhiteSpace(sourceFolder) || string.IsNullOrWhiteSpace(destinationFolder))
{
    Console.WriteLine("Configuration is incomplete. Check appsettings.json.");
    return;
}

using var httpClient = new HttpClient();
var geminiService = new GeminiService(httpClient, apiKey, modelName);
var fileProcessor = new FileProcessor(geminiService, sourceFolder, destinationFolder);

await fileProcessor.ProcessAsync();
