#if WINDOWS
// Windows AI OCR requires Microsoft.Windows.AI.Imaging (Windows 11 ARM64+).
// Uncomment when the Windows AI SDK NuGet is added to the project.
/*
using Microsoft.Windows.AI.Imaging;
using System.Text;
using Windows.Foundation;

namespace TextGrab.Models;

/// <summary>
/// Adapter wrapping Microsoft.Windows.AI.Imaging.RecognizedText into IOcrLinesWords.
/// </summary>
public class WinAiOcrLinesWords : IOcrLinesWords
{
    public WinAiOcrLinesWords(RecognizedText recognizedText)
    {
        OriginalRecognizedText = recognizedText;
        Angle = recognizedText.TextAngle;
        StringBuilder sb = new();

        if (recognizedText.Lines is not null)
        {
            Lines = Array.ConvertAll(recognizedText.Lines, line => (IOcrLine)new WinAiOcrLine(line));

            foreach (RecognizedLine recognizedLine in recognizedText.Lines)
                sb.AppendLine(recognizedLine.Text);
        }
        else
        {
            Lines = [];
        }

        Text = sb.ToString().Trim();
    }

    public RecognizedText OriginalRecognizedText { get; }
    public string Text { get; set; }
    public float Angle { get; set; }
    public IOcrLine[] Lines { get; set; }
}

public class WinAiOcrLine : IOcrLine
{
    public WinAiOcrLine(RecognizedLine recognizedLine)
    {
        OriginalLine = recognizedLine;
        Text = recognizedLine.Text;
        Words = Array.ConvertAll(recognizedLine.Words, word => (IOcrWord)new WinAiOcrWord(word));
        BoundingBox = new Rect(
            recognizedLine.BoundingBox.TopLeft,
            recognizedLine.BoundingBox.BottomRight);
    }

    public RecognizedLine OriginalLine { get; }
    public string Text { get; set; }
    public IOcrWord[] Words { get; set; }
    public Rect BoundingBox { get; set; }
}

public class WinAiOcrWord : IOcrWord
{
    public WinAiOcrWord(RecognizedWord recognizedWord)
    {
        OriginalWord = recognizedWord;
        Text = recognizedWord.Text;
        BoundingBox = new Rect(
            recognizedWord.BoundingBox.TopLeft,
            recognizedWord.BoundingBox.BottomRight);
    }

    public RecognizedWord OriginalWord { get; }
    public string Text { get; set; }
    public Rect BoundingBox { get; set; }
}
*/
#endif
