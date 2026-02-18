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

    public async Task<string> ExtractIdentityDocumentJsonAsync(string filePath)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        var base64 = Convert.ToBase64String(bytes);
        var mimeType = GetMimeType(filePath);
        var prompt = "Analyze this identity document image and detect whether it is a Passport or a NationalIdentityCard. Return only a valid JSON object with exactly two top-level keys: DesiredResult and RawData. DesiredResult must be an object with keys: DocumentType, Passport, NationalIdentityCard. DocumentType must be one of Passport, NationalIdentityCard, or Unknown. Passport must be an object with keys: PassportNumber, FullName, Nationality, DateOfBirth, PlaceOfBirth, DateOfIssue, DateOfExpiry, Gender. NationalIdentityCard must be an object with keys: IdentityNumber, FullName, PlaceOfBirth, DateOfBirth, Gender, Address, Nationality, MaritalStatus, Occupation, ValidUntil. Use null for missing values. RawData must contain all detectable raw information from the image, including MRZ lines, visible text fragments, document labels, numbers, dates, and any additional fields even if uncertain.";

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
            throw new InvalidOperationException("Gemini response does not contain candidates.");
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) || !content.TryGetProperty("parts", out var parts))
        {
            throw new InvalidOperationException("Gemini response does not contain content parts.");
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
            throw new InvalidOperationException("Gemini response text is empty.");
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
            throw new InvalidOperationException("No valid JSON was found in the AI response.");
        }

        return cleaned[firstBrace..(lastBrace + 1)].Trim();
    }

    private static void ValidateJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("DesiredResult", out _))
            {
                throw new InvalidOperationException("Extracted JSON does not contain the DesiredResult key.");
            }

            if (!doc.RootElement.TryGetProperty("RawData", out _))
            {
                throw new InvalidOperationException("Extracted JSON does not contain the RawData key.");
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Extracted JSON is invalid: {ex.Message}");
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
