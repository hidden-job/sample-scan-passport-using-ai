using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace sample_scan_passport_using_ai.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelName;

    public GeminiService(HttpClient httpClient, string apiKey, string modelName)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _modelName = modelName;
    }

    public async Task<string> ExtractPassportJsonAsync(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        var base64 = Convert.ToBase64String(bytes);
        var mimeType = GetMimeType(filePath);
        var prompt = "Extract all common passport fields from this image. Return only valid JSON object with keys: PassportNumber, FullName, Nationality, DateOfBirth, PlaceOfBirth, DateOfIssue, DateOfExpiry, Gender.";

        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new
                        {
                            text = prompt
                        },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = base64
                            }
                        }
                    }
                }
            }
        };

        var requestUri = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";
        var jsonContent = JsonSerializer.Serialize(payload);
        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync(requestUri, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini API error: {(int)response.StatusCode} - {responseBody}");
        }

        var rawText = ExtractTextFromGeminiResponse(responseBody);
        var normalized = ExtractJsonOnly(rawText);
        ValidateJson(normalized);

        return normalized;
    }

    private static string ExtractTextFromGeminiResponse(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini response tidak memiliki candidates.");
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
        {
            throw new InvalidOperationException("Gemini response tidak memiliki content parts.");
        }

        var builder = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (part.TryGetProperty("text", out var textElement))
            {
                builder.Append(textElement.GetString());
            }
        }

        var text = builder.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new InvalidOperationException("Gemini response text kosong.");
        }

        return text;
    }

    private static string ExtractJsonOnly(string rawText)
    {
        var cleaned = rawText.Trim();
        cleaned = Regex.Replace(cleaned, "^```json\\s*|^```\\s*|\\s*```$", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Multiline).Trim();

        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');

        if (firstBrace < 0 || lastBrace < 0 || lastBrace <= firstBrace)
        {
            throw new InvalidOperationException("Tidak ditemukan JSON valid pada respons AI.");
        }

        return cleaned[firstBrace..(lastBrace + 1)].Trim();
    }

    private static void ValidateJson(string json)
    {
        try
        {
            using var _ = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"JSON hasil ekstraksi tidak valid: {ex.Message}");
        }
    }

    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
