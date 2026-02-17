namespace sample_scan_passport_using_ai.Services;

public class FileProcessor
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png"];

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

        var files = Directory
            .EnumerateFiles(_sourceFolder)
            .Where(file => AllowedExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("Tidak ada file gambar passport di folder sumber.");
            return;
        }

        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            Console.WriteLine($"Memproses file: {fileName}");

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

                Console.WriteLine($"Sukses: {fileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gagal memproses {fileName}: {ex.Message}");
            }
        }
    }
}
