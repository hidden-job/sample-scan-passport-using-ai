using System.Diagnostics;

namespace sample_scan_passport_using_ai.Services;

public class FileProcessor
{
    private static readonly string[] AllowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

    private readonly GeminiService _geminiService;
    private readonly string _sourceFolder;
    private readonly string _destinationFolder;

    public FileProcessor(GeminiService geminiService, string sourceFolder, string destinationFolder)
    {
        _geminiService = geminiService;
        _sourceFolder = sourceFolder;
        _destinationFolder = destinationFolder;
    }

    public async Task ProcessAsync()
    {
        Directory.CreateDirectory(_sourceFolder);
        Directory.CreateDirectory(_destinationFolder);

        var files = await WaitForPassportImagesAsync();

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            Console.WriteLine($"Processing file: {fileName}");

            try
            {
                var extractedJson = await _geminiService.ExtractPassportJsonAsync(file);
                var jsonFileName = $"{Path.GetFileNameWithoutExtension(fileName)}.json";
                var jsonTargetPath = Path.Combine(_destinationFolder, jsonFileName);

                await File.WriteAllTextAsync(jsonTargetPath, extractedJson);

                var imageTargetPath = Path.Combine(_destinationFolder, fileName);
                if (File.Exists(imageTargetPath))
                {
                    File.Delete(imageTargetPath);
                }

                File.Move(file, imageTargetPath);

                Console.WriteLine($"Success: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to process {fileName}: {ex.Message}");
            }
        }
    }

    private async Task<List<string>> WaitForPassportImagesAsync()
    {
        while (true)
        {
            var files = Directory
                .EnumerateFiles(_sourceFolder)
                .Where(file => AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (files.Count > 0)
            {
                return files;
            }

            Console.WriteLine("No passport image files were found in the source folder.");
            OpenSourceFolderInExplorer();
            Console.WriteLine("Please put passport images in the source folder, then press Enter to continue...");
            await Task.Run(Console.ReadLine);
        }
    }

    private void OpenSourceFolderInExplorer()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = _sourceFolder,
                UseShellExecute = true
            });
        }
        catch
        {
            Console.WriteLine($"Unable to open Windows Explorer automatically. Please open this folder manually: {_sourceFolder}");
        }
    }
}
