using Microsoft.Extensions.Configuration;
using sample_scan_passport_using_ai.Services;

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
    Console.WriteLine("Konfigurasi tidak lengkap. Periksa appsettings.json.");
    return;
}

using var httpClient = new HttpClient();
var geminiService = new GeminiService(httpClient, apiKey, modelName);
var fileProcessor = new FileProcessor(geminiService, sourceFolder, destinationFolder);

await fileProcessor.ProcessAsync();
