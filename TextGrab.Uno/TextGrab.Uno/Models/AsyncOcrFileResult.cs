namespace TextGrab.Models;

public record AsyncOcrFileResult
{
    public string FilePath { get; init; }
    public string? OcrResult { get; set; }

    public AsyncOcrFileResult(string filePath)
    {
        FilePath = filePath;
    }
}
