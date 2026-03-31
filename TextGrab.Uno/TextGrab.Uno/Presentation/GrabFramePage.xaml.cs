using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using TextGrab.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace TextGrab.Presentation;

public sealed partial class GrabFramePage : Page, IGrabFrameHost
{
    private readonly ObservableCollection<WordBorder> _wordBorders = [];
    private IOcrLinesWords? _ocrResult;
    private bool _isSelecting;
    private Point _clickedPoint;
    private bool _isCtrlDown;
    private bool _isShiftDown;
    private bool _isTableMode;
    private bool _isEditMode = true;
    private DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private byte[]? _currentImageData;

    public GrabFramePage()
    {
        InitializeComponent();
        _searchTimer.Tick += SearchTimer_Tick;
    }

    // --- IGrabFrameHost ---

    public bool IsCtrlDown => _isCtrlDown;

    public void WordChanged()
    {
        UpdateFrameText();
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    public void UndoableWordChange(WordBorder wb, string oldWord)
    {
        // TODO: Wire into undo/redo system in future phase
        UpdateFrameText();
    }

    public void MergeSelectedWordBorders() => MergeSelected_Click(this, new RoutedEventArgs());

    public void BreakWordBorderIntoWords(WordBorder wb)
    {
        if (string.IsNullOrEmpty(wb.Word))
            return;

        string[] words = wb.Word.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1) return;

        double wordWidth = wb.Width / words.Length;
        for (int i = 0; i < words.Length; i++)
        {
            var newWb = new WordBorder
            {
                Word = words[i],
                Width = wordWidth,
                Height = wb.Height,
                Left = wb.Left + (i * wordWidth),
                Top = wb.Top,
                LineNumber = wb.LineNumber,
                Host = this,
            };

            if (_isEditMode) newWb.EnterEdit();
            _wordBorders.Add(newWb);
            RectanglesCanvas.Children.Add(newWb);
        }

        RemoveWordBorder(wb);
    }

    public void SearchForSimilar(WordBorder wb)
    {
        SearchBox.Text = Regex.Escape(wb.Word);
    }

    public void DeleteWordBorder(WordBorder wb) => RemoveWordBorder(wb);

    public void StartWordBorderMoveResize(WordBorder wb, Side side)
    {
        // TODO: Wire into move/resize logic in future phase
    }

    // --- Image loading ---

    private async void OpenImage_Click(object sender, RoutedEventArgs e)
    {
        var fileService = GetService<IFileService>();
        if (fileService is null) return;

        var data = await fileService.PickImageFileAsync();
        if (data is null || data.Length == 0) return;

        await LoadImage(data);
    }

    private async void PasteImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPackage = Clipboard.GetContent();
            if (!dataPackage.Contains(StandardDataFormats.Bitmap))
            {
                StatusBarText.Text = "No image in clipboard";
                return;
            }

            var streamRef = await dataPackage.GetBitmapAsync();
            using var stream = await streamRef.OpenReadAsync();
            using var memStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memStream);

            await LoadImage(memStream.ToArray());
        }
        catch (Exception ex)
        {
            StatusBarText.Text = $"Paste failed: {ex.Message}";
        }
    }

    private async Task LoadImage(byte[] imageData)
    {
        _currentImageData = imageData;

        // Display image
        var bitmapImage = new BitmapImage();
        using var ms = new MemoryStream(imageData);
        var ras = ms.AsRandomAccessStream();
        await bitmapImage.SetSourceAsync(ras);

        GrabFrameImage.Source = bitmapImage;

        // Size the canvas to match the image
        RectanglesCanvas.Width = bitmapImage.PixelWidth;
        RectanglesCanvas.Height = bitmapImage.PixelHeight;
        CanvasContainer.Width = bitmapImage.PixelWidth;
        CanvasContainer.Height = bitmapImage.PixelHeight;

        EmptyStateOverlay.Visibility = Visibility.Collapsed;

        // Populate languages
        PopulateLanguages();

        // Run OCR
        await RunOcr();
    }

    private void PopulateLanguages()
    {
        var langService = GetService<ILanguageService>();
        if (langService is null) return;

        var languages = langService.GetAllLanguages();
        LanguagesComboBox.ItemsSource = languages;

        var currentLang = langService.GetOcrLanguage();
        for (int i = 0; i < languages.Count; i++)
        {
            if (languages[i].LanguageTag == currentLang.LanguageTag)
            {
                LanguagesComboBox.SelectedIndex = i;
                break;
            }
        }

        if (LanguagesComboBox.SelectedIndex < 0 && languages.Count > 0)
            LanguagesComboBox.SelectedIndex = 0;
    }

    // --- OCR ---

    private async Task RunOcr()
    {
        if (_currentImageData is null || _currentImageData.Length == 0)
            return;

        var ocrService = GetService<IOcrService>();
        if (ocrService is null) return;

        StatusBarText.Text = "Running OCR...";
        ClearWordBorders();

        ILanguage? language = LanguagesComboBox.SelectedItem as ILanguage;

        using var stream = new MemoryStream(_currentImageData);
        var result = await ocrService.RecognizeAsync(stream, language);

        if (result?.StructuredResult is null)
        {
            StatusBarText.Text = "OCR returned no results";
            return;
        }

        _ocrResult = result.StructuredResult;
        CreateWordBordersFromOcr(_ocrResult);

        StatusBarText.Text = $"{_wordBorders.Count} words found ({result.Engine})";
    }

    private void CreateWordBordersFromOcr(IOcrLinesWords ocrResult)
    {
        int lineNumber = 0;

        foreach (IOcrLine ocrLine in ocrResult.Lines)
        {
            bool isSpaceJoining = true;
            var lang = LanguagesComboBox.SelectedItem as ILanguage;
            if (lang is not null)
                isSpaceJoining = lang.IsSpaceJoining();

            if (isSpaceJoining)
            {
                // Create one WordBorder per word
                foreach (IOcrWord ocrWord in ocrLine.Words)
                {
                    var wb = CreateWordBorderFromOcrWord(ocrWord, lineNumber);
                    _wordBorders.Add(wb);
                    RectanglesCanvas.Children.Add(wb);
                }
            }
            else
            {
                // CJK: Create one WordBorder per line
                var box = ocrLine.BoundingBox;
                if (box.Width > 0 && box.Height > 0)
                {
                    StringBuilder lineText = new();
                    ocrLine.GetTextFromOcrLine(false, lineText);

                    var wb = new WordBorder
                    {
                        Word = lineText.ToString().TrimEnd(),
                        Width = box.Width,
                        Height = box.Height,
                        Left = box.X,
                        Top = box.Y,
                        LineNumber = lineNumber,
                        Host = this,
                    };
                    if (_isEditMode) wb.EnterEdit();
                    _wordBorders.Add(wb);
                    RectanglesCanvas.Children.Add(wb);
                }
            }

            lineNumber++;
        }

        UpdateFrameText();
    }

    private WordBorder CreateWordBorderFromOcrWord(IOcrWord ocrWord, int lineNumber)
    {
        var box = ocrWord.BoundingBox;
        var wb = new WordBorder
        {
            Word = ocrWord.Text,
            Width = Math.Max(box.Width, 10),
            Height = Math.Max(box.Height, 10),
            Left = box.X,
            Top = box.Y,
            LineNumber = lineNumber,
            Host = this,
        };

        if (_isEditMode)
            wb.EnterEdit();

        return wb;
    }

    // --- Canvas interaction ---

    private void RectanglesCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _clickedPoint = e.GetCurrentPoint(RectanglesCanvas).Position;
        _isSelecting = true;

        // Show selection border
        Canvas.SetLeft(SelectBorder, _clickedPoint.X);
        Canvas.SetTop(SelectBorder, _clickedPoint.Y);
        SelectBorder.Width = 0;
        SelectBorder.Height = 0;
        SelectBorder.Visibility = Visibility.Visible;

        RectanglesCanvas.CapturePointer(e.Pointer);

        // If no shift, deselect all first
        if (!_isShiftDown)
            DeselectAll();

        e.Handled = true;
    }

    private void RectanglesCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isSelecting) return;

        var currentPoint = e.GetCurrentPoint(RectanglesCanvas).Position;

        double x = Math.Min(_clickedPoint.X, currentPoint.X);
        double y = Math.Min(_clickedPoint.Y, currentPoint.Y);
        double w = Math.Abs(currentPoint.X - _clickedPoint.X);
        double h = Math.Abs(currentPoint.Y - _clickedPoint.Y);

        Canvas.SetLeft(SelectBorder, x);
        Canvas.SetTop(SelectBorder, y);
        SelectBorder.Width = w;
        SelectBorder.Height = h;

        // Live-check intersections
        if (w > 4 || h > 4)
            CheckSelectBorderIntersections();
    }

    private void RectanglesCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isSelecting = false;
        RectanglesCanvas.ReleasePointerCapture(e.Pointer);

        CheckSelectBorderIntersections();

        SelectBorder.Visibility = Visibility.Collapsed;
        UpdateFrameText();
    }

    private void CheckSelectBorderIntersections()
    {
        double x = Canvas.GetLeft(SelectBorder);
        double y = Canvas.GetTop(SelectBorder);
        Rect selectRect = new(x, y, SelectBorder.Width, SelectBorder.Height);

        if (selectRect.Width < 4 && selectRect.Height < 4)
            return;

        foreach (var wb in _wordBorders)
        {
            if (wb.IntersectsWith(selectRect))
                wb.Select();
            else if (!_isShiftDown)
                wb.Deselect();
        }
    }

    // --- Keyboard ---

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Control) _isCtrlDown = true;
        if (e.Key == Windows.System.VirtualKey.Shift) _isShiftDown = true;
    }

    private void Page_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Control) _isCtrlDown = false;
        if (e.Key == Windows.System.VirtualKey.Shift) _isShiftDown = false;
    }

    // --- Selection ---

    private List<WordBorder> SelectedWordBorders()
        => _wordBorders.Where(wb => wb.IsSelected).ToList();

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var wb in _wordBorders)
            wb.Select();
        UpdateFrameText();
    }

    private void InvertSelection_Click(object sender, RoutedEventArgs e)
    {
        foreach (var wb in _wordBorders)
        {
            if (wb.IsSelected) wb.Deselect();
            else wb.Select();
        }
        UpdateFrameText();
    }

    private void DeselectAll()
    {
        foreach (var wb in _wordBorders)
            wb.Deselect();
    }

    // --- Copy ---

    private void CopyText_Click(object sender, RoutedEventArgs e)
    {
        UpdateFrameText();
        var selected = SelectedWordBorders();
        string text = selected.Count > 0
            ? string.Join(Environment.NewLine, selected.Select(wb => wb.Word))
            : string.Join(Environment.NewLine, _wordBorders.Select(wb => wb.Word));

        if (!string.IsNullOrEmpty(text))
        {
            var dp = new DataPackage();
            dp.SetText(text);
            Clipboard.SetContent(dp);
            StatusBarText.Text = "Copied to clipboard";
        }
    }

    private void SendToEditWindow_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to EditText with the OCR text
        UpdateFrameText();
        var selected = SelectedWordBorders();
        string text = selected.Count > 0
            ? string.Join(Environment.NewLine, selected.Select(wb => wb.Word))
            : string.Join(Environment.NewLine, _wordBorders.Select(wb => wb.Word));

        // TODO: Navigate with data to EditTextPage
        StatusBarText.Text = "Sent to Edit Window";
    }

    // --- Word management ---

    private void MergeSelected_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedWordBorders();
        if (selected.Count < 2) return;
        PushUndo();

        // Compute merged bounds
        double left = selected.Min(wb => wb.Left);
        double top = selected.Min(wb => wb.Top);
        double right = selected.Max(wb => wb.Right);
        double bottom = selected.Max(wb => wb.Bottom);

        // Build merged text (sorted by position)
        var sorted = selected.OrderBy(wb => wb.Top).ThenBy(wb => wb.Left);
        string mergedText = string.Join(" ", sorted.Select(wb => wb.Word));

        // Remove old borders
        foreach (var wb in selected)
            RemoveWordBorder(wb);

        // Create merged border
        var merged = new WordBorder
        {
            Word = mergedText,
            Left = left,
            Top = top,
            Width = right - left,
            Height = bottom - top,
            Host = this,
        };
        if (_isEditMode) merged.EnterEdit();
        merged.Select();
        _wordBorders.Add(merged);
        RectanglesCanvas.Children.Add(merged);

        UpdateFrameText();
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        var selected = SelectedWordBorders().ToList();
        foreach (var wb in selected)
            RemoveWordBorder(wb);
        UpdateFrameText();
    }

    private void RemoveWordBorder(WordBorder wb)
    {
        _wordBorders.Remove(wb);
        RectanglesCanvas.Children.Remove(wb);
    }

    private void ClearWordBorders()
    {
        foreach (var wb in _wordBorders.ToList())
            RectanglesCanvas.Children.Remove(wb);
        _wordBorders.Clear();
    }

    // --- Refresh OCR ---

    private async void RefreshOcr_Click(object sender, RoutedEventArgs e)
    {
        await RunOcr();
    }

    private void LanguagesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Language changed — user can hit Refresh to re-OCR
    }

    // --- Edit / Table mode ---

    private void EditModeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isEditMode = EditToggleButton.IsChecked == true || EditModeToggle.IsChecked;
        EditToggleButton.IsChecked = _isEditMode;
        EditModeToggle.IsChecked = _isEditMode;

        foreach (var wb in _wordBorders)
        {
            if (_isEditMode) wb.EnterEdit();
            else wb.ExitEdit();
        }
    }

    private void TableModeToggle_Click(object sender, RoutedEventArgs e)
    {
        _isTableMode = TableToggleButton.IsChecked == true || TableModeToggle.IsChecked;
        TableToggleButton.IsChecked = _isTableMode;
        TableModeToggle.IsChecked = _isTableMode;
        // TODO: Wire table analysis from ResultTable in future
    }

    // --- Search ---

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void SearchTimer_Tick(object? sender, object e)
    {
        _searchTimer.Stop();
        ApplySearch();
    }

    private void ApplySearch()
    {
        string searchText = SearchBox.Text;
        int matchCount = 0;

        if (string.IsNullOrEmpty(searchText))
        {
            foreach (var wb in _wordBorders)
                wb.Deselect();
            MatchCountText.Text = "0 matches";
            return;
        }

        try
        {
            var regex = new Regex(searchText, RegexOptions.IgnoreCase);

            foreach (var wb in _wordBorders)
            {
                if (regex.IsMatch(wb.Word))
                {
                    wb.Select();
                    matchCount++;
                }
                else
                {
                    wb.Deselect();
                }
            }
        }
        catch (RegexParseException)
        {
            // Invalid regex — try literal match
            foreach (var wb in _wordBorders)
            {
                if (wb.Word.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                {
                    wb.Select();
                    matchCount++;
                }
                else
                {
                    wb.Deselect();
                }
            }
        }

        MatchCountText.Text = $"{matchCount} match{(matchCount != 1 ? "es" : "")}";
    }

    // --- Helpers ---

    private void UpdateFrameText()
    {
        var selected = SelectedWordBorders();
        var words = selected.Count > 0 ? selected : _wordBorders.ToList();
        var sorted = words.OrderBy(wb => wb.LineNumber).ThenBy(wb => wb.Left);

        StringBuilder sb = new();
        int prevLine = -1;

        foreach (var wb in sorted)
        {
            if (prevLine >= 0 && wb.LineNumber != prevLine)
                sb.AppendLine();
            else if (prevLine >= 0)
                sb.Append(' ');

            sb.Append(wb.Word);
            prevLine = wb.LineNumber;
        }

        // Keep frame text available for copy
        StatusBarText.Text = $"{_wordBorders.Count} words | {selected.Count} selected";
    }

    // --- Undo/Redo ---

    private readonly Stack<List<WordBorderInfo>> _undoStack = new();
    private readonly Stack<List<WordBorderInfo>> _redoStack = new();

    private void PushUndo()
    {
        _undoStack.Push(CaptureCurrentState());
        _redoStack.Clear();
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (_undoStack.Count == 0) return;
        _redoStack.Push(CaptureCurrentState());
        RestoreWordBorders(_undoStack.Pop());
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (_redoStack.Count == 0) return;
        _undoStack.Push(CaptureCurrentState());
        RestoreWordBorders(_redoStack.Pop());
    }

    private List<WordBorderInfo> CaptureCurrentState()
    {
        return _wordBorders.Select(wb => wb.ToInfo()).ToList();
    }

    private void RestoreWordBorders(List<WordBorderInfo> state)
    {
        RectanglesCanvas.Children.Clear();
        RectanglesCanvas.Children.Add(SelectBorder);
        _wordBorders.Clear();

        foreach (var info in state)
        {
            var wb = new WordBorder(info);
            wb.Host = this;
            _wordBorders.Add(wb);
            RectanglesCanvas.Children.Add(wb);
        }

        UpdateFrameText();
    }

    // --- Text transforms on selected words ---

    private void TryToNumbers_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        foreach (var wb in _wordBorders.Where(w => w.IsSelected))
            wb.Word = wb.Word.TryFixToNumbers();
    }

    private void TryToLetters_Click(object sender, RoutedEventArgs e)
    {
        PushUndo();
        foreach (var wb in _wordBorders.Where(w => w.IsSelected))
            wb.Word = wb.Word.TryFixToLetters();
    }

    private void FreezeToggle_Click(object sender, RoutedEventArgs e)
    {
        // Freeze prevents automatic OCR refresh
        StatusBarText.Text = FreezeToggle.IsChecked ? "Frozen" : "Live";
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        StatusBarText.Text = "Text Grab v5.0 — Grab Frame";
    }

    private T? GetService<T>() where T : class
        => this.FindServiceProvider()?.GetService(typeof(T)) as T;
}
