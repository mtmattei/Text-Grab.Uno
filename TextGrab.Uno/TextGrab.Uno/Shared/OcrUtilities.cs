using System.Text;
using System.Text.RegularExpressions;
using Windows.Foundation;

namespace TextGrab.Shared;

/// <summary>
/// Portable OCR text extraction and formatting utilities.
/// Platform-specific OCR invocation lives in IOcrEngine implementations.
/// </summary>
public static partial class OcrUtilities
{
    // Max image dimension for Windows OCR engine (4096px).
    // Used for scale factor calculations.
    public const int MaxImageDimension = 4096;

    private static readonly Regex _cachedSpaceJoiningWordRegex = SpaceJoiningWordRegex();

    /// <summary>
    /// Extracts text from an OCR line, handling CJK space-joining vs space-delimited languages.
    /// </summary>
    public static void GetTextFromOcrLine(
        this IOcrLine ocrLine,
        bool isSpaceJoiningOcrLang,
        StringBuilder text,
        bool correctErrors = false,
        bool correctToLatin = false)
    {
        if (isSpaceJoiningOcrLang)
        {
            text.AppendLine(ocrLine.Text);

            if (correctErrors)
                text.TryFixEveryWordLetterNumberErrors();
        }
        else
        {
            bool isFirstWord = true;
            bool isPrevWordSpaceJoining = false;

            foreach (IOcrWord ocrWord in ocrLine.Words)
            {
                string wordString = ocrWord.Text;

                bool isThisWordSpaceJoining = _cachedSpaceJoiningWordRegex.IsMatch(wordString);

                if (correctErrors)
                    wordString = wordString.TryFixNumberLetterErrors();

                if (isFirstWord || (!isThisWordSpaceJoining && !isPrevWordSpaceJoining))
                    _ = text.Append(wordString);
                else
                    _ = text.Append(' ').Append(wordString);

                isFirstWord = false;
                isPrevWordSpaceJoining = isThisWordSpaceJoining;
            }
        }

        if (correctToLatin)
            text.ReplaceGreekOrCyrillicWithLatin();
    }

    /// <summary>
    /// Converts structured OCR result into a text OcrOutput with formatting.
    /// </summary>
    public static OcrOutput GetTextFromOcrResult(
        IOcrLinesWords ocrResult,
        ILanguage language,
        bool correctErrors = false,
        bool correctToLatin = false)
    {
        StringBuilder text = new();
        bool isSpaceJoining = language.IsSpaceJoining();

        foreach (IOcrLine ocrLine in ocrResult.Lines)
            ocrLine.GetTextFromOcrLine(isSpaceJoining, text, correctErrors, correctToLatin);

        if (language.IsRightToLeft())
            text.ReverseWordsForRightToLeft();

        return new OcrOutput
        {
            Kind = OcrOutputKind.Paragraph,
            RawOutput = text.ToString(),
            LanguageTag = language.LanguageTag,
            StructuredResult = ocrResult,
        };
    }

    /// <summary>
    /// Combines multiple OcrOutputs into a single string, applying error correction.
    /// </summary>
    public static string GetStringFromOcrOutputs(
        List<OcrOutput> outputs,
        bool correctToLatin = false,
        bool correctErrors = false)
    {
        StringBuilder text = new();

        foreach (OcrOutput output in outputs)
        {
            output.CleanOutput(correctToLatin, correctErrors);

            if (!string.IsNullOrWhiteSpace(output.CleanedOutput))
                text.Append(output.CleanedOutput);
            else if (!string.IsNullOrWhiteSpace(output.RawOutput))
                text.Append(output.RawOutput);
        }

        return text.ToString();
    }

    /// <summary>
    /// Finds the word at a given point in OCR results.
    /// </summary>
    public static string GetTextFromClickedWord(Point clickPoint, IOcrLinesWords ocrResult)
    {
        foreach (IOcrLine ocrLine in ocrResult.Lines)
            foreach (IOcrWord ocrWord in ocrLine.Words)
                if (ocrWord.BoundingBox.Contains(clickPoint))
                    return ocrWord.Text;

        return string.Empty;
    }

    /// <summary>
    /// Calculates the ideal scale factor for OCR based on average word height.
    /// Target line height is 40px for optimal OCR accuracy.
    /// </summary>
    public static double GetIdealScaleFactor(IOcrLinesWords ocrResult, int imageHeight, int imageWidth)
    {
        List<double> heightsList = [];

        foreach (IOcrLine ocrLine in ocrResult.Lines)
            foreach (IOcrWord ocrWord in ocrLine.Words)
                heightsList.Add(ocrWord.BoundingBox.Height);

        double lineHeight = heightsList.Count > 0 ? heightsList.Average() : 10;

        const double idealLineHeight = 40.0;
        double scaleFactor = idealLineHeight / lineHeight;

        if (imageWidth * scaleFactor > MaxImageDimension || imageHeight * scaleFactor > MaxImageDimension)
        {
            int largerDim = Math.Max(imageWidth, imageHeight);
            scaleFactor = (double)MaxImageDimension / largerDim;
        }

        return scaleFactor;
    }

    // Matches words in a space-joining language context:
    // - one letter that is not CJK ("other letters")
    // - one number digit
    // - any words longer than one character
    [GeneratedRegex(@"(^[\p{L}-[\p{Lo}]]|\p{Nd}$)|.{2,}")]
    private static partial Regex SpaceJoiningWordRegex();
}
