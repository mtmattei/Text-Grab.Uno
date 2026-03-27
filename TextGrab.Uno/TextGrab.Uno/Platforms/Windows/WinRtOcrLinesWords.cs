#if WINDOWS
using Windows.Foundation;
using Windows.Media.Ocr;

namespace TextGrab.Models;

/// <summary>
/// Adapter wrapping Windows.Media.Ocr.OcrResult into the cross-platform IOcrLinesWords interface.
/// </summary>
public class WinRtOcrLinesWords : IOcrLinesWords
{
    public WinRtOcrLinesWords(OcrResult ocrResult)
    {
        OriginalOcrResult = ocrResult;
        Angle = (float)(ocrResult.TextAngle ?? 0.0f);

        Lines = new WinRtOcrLine[ocrResult.Lines.Count];
        for (int i = 0; i < ocrResult.Lines.Count; i++)
        {
            Lines[i] = new WinRtOcrLine(ocrResult.Lines[i]);
        }

        Text = ocrResult.Text;
    }

    public OcrResult OriginalOcrResult { get; }
    public string Text { get; set; }
    public float Angle { get; set; }
    public IOcrLine[] Lines { get; set; }
}

public class WinRtOcrLine : IOcrLine
{
    public WinRtOcrLine(OcrLine ocrLine)
    {
        OriginalLine = ocrLine;
        Text = ocrLine.Text;
        Words = new WinRtOcrWord[ocrLine.Words.Count];

        for (int i = 0; i < ocrLine.Words.Count; i++)
        {
            Words[i] = new WinRtOcrWord(ocrLine.Words[i]);
        }

        // Compute bounding rect from word rects
        double left = double.MaxValue, top = double.MaxValue;
        double right = double.MinValue, bottom = double.MinValue;

        foreach (OcrWord word in ocrLine.Words)
        {
            var r = word.BoundingRect;
            if (r.Left < left) left = r.Left;
            if (r.Top < top) top = r.Top;
            if (r.Right > right) right = r.Right;
            if (r.Bottom > bottom) bottom = r.Bottom;
        }

        BoundingBox = new Rect(left, top, right - left, bottom - top);
    }

    public OcrLine OriginalLine { get; }
    public string Text { get; set; }
    public IOcrWord[] Words { get; set; }
    public Rect BoundingBox { get; set; }
}

public class WinRtOcrWord : IOcrWord
{
    public WinRtOcrWord(OcrWord ocrWord)
    {
        OriginalWord = ocrWord;
        Text = ocrWord.Text;
        BoundingBox = ocrWord.BoundingRect;
    }

    public OcrWord OriginalWord { get; }
    public string Text { get; set; }
    public Rect BoundingBox { get; set; }
}
#endif
