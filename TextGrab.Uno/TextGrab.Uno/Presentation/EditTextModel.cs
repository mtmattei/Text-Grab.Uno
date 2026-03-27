using TextGrab.Models;
using TextGrab.Shared;

namespace TextGrab.Presentation;

public partial record EditTextModel
{
    private readonly INavigator _navigator;
    private readonly IOptions<AppSettings> _settings;
    private readonly IOcrService _ocrService;
    private readonly IFileService _fileService;

    public EditTextModel(
        INavigator navigator,
        IOptions<AppSettings> settings,
        IOcrService ocrService,
        IFileService fileService)
    {
        _navigator = navigator;
        _settings = settings;
        _ocrService = ocrService;
        _fileService = fileService;
    }

    // --- Core State ---

    public IState<string> Text => State<string>.Value(this, () => string.Empty);

    public IState<string> StatusText => State<string>.Value(this, () => "Ready");

    public IState<bool> IsWordWrap => State<bool>.Value(this, () => _settings.Value?.EditWindowIsWordWrapOn ?? true);

    public IState<bool> IsModified => State<bool>.Value(this, () => false);

    // --- Full-text transformation commands ---
    // These operate on the entire text content (no selection needed).
    // Selection-aware commands live in EditTextPage.xaml.cs.

    public async ValueTask MakeSingleLine()
    {
        var text = await Text;
        if (string.IsNullOrEmpty(text)) return;
        await Text.Set(text.MakeStringSingleLine(), CancellationToken.None);
        await IsModified.Set(true, CancellationToken.None);
    }

    public async ValueTask TrimEachLine()
    {
        var text = await Text;
        if (string.IsNullOrEmpty(text)) return;

        var lines = text.Split(Environment.NewLine);
        var trimmed = string.Join(Environment.NewLine, lines.Select(l => l.Trim()));
        await Text.Set(trimmed, CancellationToken.None);
        await IsModified.Set(true, CancellationToken.None);
    }

    public async ValueTask RemoveDuplicateLines()
    {
        var text = await Text;
        if (string.IsNullOrEmpty(text)) return;
        await Text.Set(text.RemoveDuplicateLines(), CancellationToken.None);
        await IsModified.Set(true, CancellationToken.None);
    }

    public async ValueTask ReplaceReservedChars()
    {
        var text = await Text;
        if (string.IsNullOrEmpty(text)) return;
        await Text.Set(text.ReplaceReservedCharacters(), CancellationToken.None);
        await IsModified.Set(true, CancellationToken.None);
    }

    public async ValueTask TryToNumbers()
    {
        var text = await Text;
        if (string.IsNullOrEmpty(text)) return;
        await Text.Set(text.TryFixToNumbers(), CancellationToken.None);
        await IsModified.Set(true, CancellationToken.None);
    }

    public async ValueTask TryToLetters()
    {
        var text = await Text;
        if (string.IsNullOrEmpty(text)) return;
        await Text.Set(text.TryFixToLetters(), CancellationToken.None);
        await IsModified.Set(true, CancellationToken.None);
    }

    public async ValueTask CorrectGuids()
    {
        var text = await Text;
        if (string.IsNullOrEmpty(text)) return;
        await Text.Set(text.CorrectCommonGuidErrors(), CancellationToken.None);
        await IsModified.Set(true, CancellationToken.None);
    }

    public async ValueTask ToggleWordWrap()
    {
        var current = await IsWordWrap;
        await IsWordWrap.Set(!current, CancellationToken.None);
    }

    // --- OCR commands ---

    public IState<bool> IsOcrBusy => State<bool>.Value(this, () => false);

    public async ValueTask OcrFromImage()
    {
        await IsOcrBusy.Set(true, CancellationToken.None);
        await StatusText.Set("Running OCR...", CancellationToken.None);

        try
        {
            byte[]? imageData = await _fileService.PickImageFileAsync();
            if (imageData is null || imageData.Length == 0)
            {
                await StatusText.Set("Ready", CancellationToken.None);
                return;
            }

            using var stream = new MemoryStream(imageData);
            var result = await _ocrService.RecognizeAsync(stream);

            if (result is null)
            {
                await StatusText.Set("OCR returned no results", CancellationToken.None);
                return;
            }

            string ocrText = !string.IsNullOrWhiteSpace(result.CleanedOutput)
                ? result.CleanedOutput
                : result.RawOutput;

            var currentText = await Text;
            if (string.IsNullOrEmpty(currentText))
                await Text.Set(ocrText, CancellationToken.None);
            else
                await Text.Set(currentText + Environment.NewLine + ocrText, CancellationToken.None);

            await IsModified.Set(true, CancellationToken.None);
            await StatusText.Set($"OCR complete ({result.Engine})", CancellationToken.None);
        }
        catch (Exception ex)
        {
            await StatusText.Set($"OCR failed: {ex.Message}", CancellationToken.None);
        }
        finally
        {
            await IsOcrBusy.Set(false, CancellationToken.None);
        }
    }

    public async ValueTask OcrFromClipboard()
    {
        await IsOcrBusy.Set(true, CancellationToken.None);
        await StatusText.Set("Reading clipboard image...", CancellationToken.None);

        try
        {
            var dataPackage = Windows.ApplicationModel.DataTransfer.Clipboard.GetContent();
            if (!dataPackage.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.Bitmap))
            {
                await StatusText.Set("No image in clipboard", CancellationToken.None);
                return;
            }

            var streamRef = await dataPackage.GetBitmapAsync();
            using var stream = await streamRef.OpenReadAsync();
            using var memStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memStream);
            memStream.Position = 0;

            var result = await _ocrService.RecognizeAsync(memStream);

            if (result is null)
            {
                await StatusText.Set("OCR returned no results", CancellationToken.None);
                return;
            }

            string ocrText = !string.IsNullOrWhiteSpace(result.CleanedOutput)
                ? result.CleanedOutput
                : result.RawOutput;

            var currentText = await Text;
            if (string.IsNullOrEmpty(currentText))
                await Text.Set(ocrText, CancellationToken.None);
            else
                await Text.Set(currentText + Environment.NewLine + ocrText, CancellationToken.None);

            await IsModified.Set(true, CancellationToken.None);
            await StatusText.Set($"OCR complete ({result.Engine})", CancellationToken.None);
        }
        catch (Exception ex)
        {
            await StatusText.Set($"OCR failed: {ex.Message}", CancellationToken.None);
        }
        finally
        {
            await IsOcrBusy.Set(false, CancellationToken.None);
        }
    }
}
