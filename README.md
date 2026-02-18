# Sample Scan Identity Document Using AI

A .NET console application that scans passport or national identity card images from a source folder, sends each image to Google Gemini for field extraction, and stores structured JSON results in a destination folder.

## Features

- Reads `.jpg`, `.jpeg`, and `.png` passport or national identity card images from a configurable source directory.
- Sends image + prompt payload to Gemini (`generateContent`).
- Normalizes model output to a pure JSON object with two keys: `DesiredResult` and `RawData`.
- Validates extracted JSON before saving.
- Writes one JSON file per image and moves processed images to the destination folder.
- Automatically creates source and destination folders if they do not exist.

## Project Structure

- `Program.cs` — app startup and configuration loading.
- `Services/GeminiService.cs` — Gemini API integration and response normalization.
- `Services/FileProcessor.cs` — file discovery, processing flow, and output handling.
- `Models/PassportResult.cs` — legacy passport field model example.
- `appsettings.json` — API and folder configuration.

## Requirements

- .NET 6 SDK (or compatible runtime for this project).
- A valid Google Gemini API key.

## Configuration

Edit `appsettings.json`:

```json
{
  "Gemini": {
    "ApiKey": "YOUR_API_KEY",
    "ModelName": "gemini-2.5-flash"
  },
  "Folder": {
    "SourceFolder": "temp/identity-source",
    "DestinationFolder": "temp/identity-processed"
  }
}
```

## How to Run

```bash
dotnet restore
dotnet run
```

## Output Behavior

For each supported image in the source folder:

1. The app extracts identity document data into a JSON object with:
   - `DesiredResult` for standardized fields.
   - `RawData` for all detectable raw content from the image.
2. It creates a JSON file with the same base filename in the destination folder.
3. It moves the original image to the destination folder.

Example:

- Input image: `identity_01.jpg`
- Output JSON: `identity_01.json`
- Moved image: `identity_01.jpg` (inside destination folder)

## Notes

- If the source folder has no supported image files, the app opens the source folder in Windows Explorer and waits for you to press Enter after adding images.
- If Gemini returns invalid or non-JSON text, the app throws an error for that file and continues with the next file.
