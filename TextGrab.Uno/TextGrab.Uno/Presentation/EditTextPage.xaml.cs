using TextGrab.Services;
using TextGrab.Shared;
using Windows.ApplicationModel.DataTransfer;

namespace TextGrab.Presentation;

public sealed partial class EditTextPage : Page
{
    private CurrentCase _caseStatus = CurrentCase.Unknown;
    private string? _openedFilePath;
    private IFileService? _fileService;

    public EditTextPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _fileService = this.FindServiceProvider()?.GetService(typeof(IFileService)) as IFileService;
    }

    private INavigator? Navigator => this.FindServiceProvider()?.GetService(typeof(INavigator)) as INavigator;

    // --- Status bar updates ---

    private void PassedTextControl_SelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateLineAndColumn();
    }

    private void PassedTextControl_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateWordAndCharCount();
        UpdateLineAndColumn();
    }

    private void UpdateLineAndColumn()
    {
        var text = PassedTextControl.Text;
        int pos = PassedTextControl.SelectionStart;

        int line = 1;
        int col = 0;
        for (int i = 0; i < pos && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                col = 0;
            }
            else if (text[i] != '\r')
            {
                col++;
            }
        }

        LineColText.Text = $"Ln {line}, Col {col}";
    }

    private void UpdateWordAndCharCount()
    {
        var text = PassedTextControl.Text;
        CharCountText.Text = $"Chars: {text.Length}";

        int wordCount = string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        WordCountText.Text = $"Words: {wordCount}";
    }

    // --- Clipboard / Edit commands ---

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength > 0)
        {
            var dp = new DataPackage();
            dp.SetText(PassedTextControl.SelectedText);
            Clipboard.SetContent(dp);
            ReplaceSelection(string.Empty);
        }
    }

    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength > 0)
        {
            var dp = new DataPackage();
            dp.SetText(PassedTextControl.SelectedText);
            Clipboard.SetContent(dp);
        }
    }

    private async void Paste_Click(object sender, RoutedEventArgs e)
    {
        var content = Clipboard.GetContent();
        if (content.Contains(StandardDataFormats.Text))
        {
            var text = await content.GetTextAsync();
            ReplaceSelection(text);
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        // TextBox built-in undo
        // WinUI TextBox doesn't expose Undo programmatically; rely on Ctrl+Z
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        // TextBox built-in redo
    }

    private void CopyClose_Click(object sender, RoutedEventArgs e)
    {
        var dp = new DataPackage();
        dp.SetText(PassedTextControl.Text);
        Clipboard.SetContent(dp);
        StatusBarText.Text = "Copied to clipboard";
    }

    // --- Text transform commands (full text, delegate to model/StringMethods) ---

    private void MakeSingleLine_Click(object sender, RoutedEventArgs e)
    {
        PassedTextControl.Text = PassedTextControl.Text.MakeStringSingleLine();
    }

    private void TrimEachLine_Click(object sender, RoutedEventArgs e)
    {
        var lines = PassedTextControl.Text.Split('\n');
        PassedTextControl.Text = string.Join('\n', lines.Select(l => l.TrimEnd('\r').Trim()));
    }

    private void TryToNumbers_Click(object sender, RoutedEventArgs e)
    {
        ApplyToSelectionOrAll(t => t.TryFixToNumbers());
    }

    private void TryToLetters_Click(object sender, RoutedEventArgs e)
    {
        ApplyToSelectionOrAll(t => t.TryFixToLetters());
    }

    private void CorrectGuids_Click(object sender, RoutedEventArgs e)
    {
        ApplyToSelectionOrAll(t => t.CorrectCommonGuidErrors());
    }

    private void RemoveDuplicateLines_Click(object sender, RoutedEventArgs e)
    {
        PassedTextControl.Text = PassedTextControl.Text.RemoveDuplicateLines();
    }

    private void ReplaceReservedChars_Click(object sender, RoutedEventArgs e)
    {
        ApplyToSelectionOrAll(t => t.ReplaceReservedCharacters());
    }

    // --- Selection-aware commands ---

    private void ToggleCase_Click(object sender, RoutedEventArgs e)
    {
        string textToModify = PassedTextControl.SelectionLength > 0
            ? PassedTextControl.SelectedText
            : PassedTextControl.Text;

        if (string.IsNullOrEmpty(textToModify)) return;

        _caseStatus = StringMethods.DetermineToggleCase(textToModify);

        string result = _caseStatus switch
        {
            CurrentCase.Lower => textToModify.ToUpperInvariant(),
            CurrentCase.Upper => textToModify.ToLowerInvariant(),
            _ => textToModify.ToLowerInvariant()
        };

        if (PassedTextControl.SelectionLength > 0)
            ReplaceSelection(result);
        else
            PassedTextControl.Text = result;
    }

    private void Unstack_Click(object sender, RoutedEventArgs e)
    {
        string text = PassedTextControl.Text;
        if (string.IsNullOrEmpty(text)) return;

        // Count columns from first line (space-separated)
        string firstLine = text.Split('\n')[0].TrimEnd('\r');
        int cols = firstLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
        if (cols < 2) cols = 2;

        PassedTextControl.Text = text.UnstackStrings(cols);
    }

    private void UnstackGroup_Click(object sender, RoutedEventArgs e)
    {
        string text = PassedTextControl.Text;
        if (string.IsNullOrEmpty(text)) return;

        int rows = text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        int groups = Math.Max(2, rows / 2);

        PassedTextControl.Text = text.UnstackGroups(groups);
    }

    private void SelectWord_Click(object sender, RoutedEventArgs e)
    {
        var text = PassedTextControl.Text;
        int pos = PassedTextControl.SelectionStart;
        if (string.IsNullOrEmpty(text) || pos >= text.Length) return;

        var (start, length) = text.CursorWordBoundaries(pos);
        PassedTextControl.Select(start, length);
    }

    private void SelectLine_Click(object sender, RoutedEventArgs e)
    {
        var text = PassedTextControl.Text;
        int pos = PassedTextControl.SelectionStart;
        if (string.IsNullOrEmpty(text)) return;

        var (start, length) = text.GetStartAndLengthOfLineAtPosition(pos);
        PassedTextControl.Select(start, length);
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e)
    {
        PassedTextControl.SelectAll();
    }

    private void SelectNone_Click(object sender, RoutedEventArgs e)
    {
        PassedTextControl.Select(PassedTextControl.Text.Length, 0);
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength > 0)
            ReplaceSelection(string.Empty);
    }

    private void IsolateSelection_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength > 0)
            PassedTextControl.Text = PassedTextControl.SelectedText;
    }

    private void DeleteAllSelection_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength == 0) return;

        string selection = PassedTextControl.SelectedText;
        PassedTextControl.Text = PassedTextControl.Text.RemoveAllInstancesOf(selection);
    }

    private void InsertOnEveryLine_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength == 0) return;

        string selection = PassedTextControl.SelectedText;
        int cursorCol = GetCurrentColumn();

        var lines = PassedTextControl.Text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            if (cursorCol <= line.Length)
                lines[i] = line.Insert(cursorCol, selection);
            else
                lines[i] = line.PadRight(cursorCol) + selection;
        }

        PassedTextControl.Text = string.Join('\n', lines);
    }

    private void SplitOnSelection_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength == 0) return;
        string selection = PassedTextControl.SelectedText;
        PassedTextControl.Text = PassedTextControl.Text.Replace(selection, Environment.NewLine + selection);
    }

    private void SplitAfterSelection_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength == 0) return;
        string selection = PassedTextControl.SelectedText;
        PassedTextControl.Text = PassedTextControl.Text.Replace(selection, selection + Environment.NewLine);
    }

    private void MoveLineUp_Click(object sender, RoutedEventArgs e)
    {
        MoveCurrentLine(-1);
    }

    private void MoveLineDown_Click(object sender, RoutedEventArgs e)
    {
        MoveCurrentLine(1);
    }

    // --- Format ---

    private void WrapText_Click(object sender, RoutedEventArgs e)
    {
        bool isChecked = WrapTextToggle.IsChecked;
        PassedTextControl.TextWrapping = isChecked ? TextWrapping.Wrap : TextWrapping.NoWrap;
    }

    private void DeleteAllSelectionPattern_Click(object sender, RoutedEventArgs e)
    {
        if (PassedTextControl.SelectionLength == 0) return;
        string selection = PassedTextControl.SelectedText;
        // Remove all lines containing the selection pattern
        var lines = PassedTextControl.Text.Split('\n');
        var filtered = lines.Where(l => !l.Contains(selection));
        PassedTextControl.Text = string.Join('\n', filtered);
    }

    private async void AddRemoveAt_Click(object sender, RoutedEventArgs e)
    {
        // Simple add/remove dialog using ContentDialog
        var inputBox = new TextBox { PlaceholderText = "Text to add/remove" };
        var posBox = new TextBox { PlaceholderText = "Position (0 = start, -1 = end)" };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Add text at position on every line:" });
        panel.Children.Add(inputBox);
        panel.Children.Add(posBox);

        var dialog = new ContentDialog
        {
            Title = "Add or Remove at Position",
            Content = panel,
            PrimaryButtonText = "Add",
            SecondaryButtonText = "Remove",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.None || string.IsNullOrEmpty(inputBox.Text)) return;

        int.TryParse(posBox.Text, out int position);
        var lines = PassedTextControl.Text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            if (result == ContentDialogResult.Primary) // Add
            {
                int insertAt = position < 0 ? line.Length : Math.Min(position, line.Length);
                lines[i] = line.Insert(insertAt, inputBox.Text);
            }
            else // Remove
            {
                lines[i] = line.Replace(inputBox.Text, "");
            }
        }

        PassedTextControl.Text = string.Join('\n', lines);
    }

    private async void RegexManager_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Dialogs.RegexManagerDialog { XamlRoot = this.XamlRoot };
        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.SelectedPattern is not null)
        {
            // Open Find & Replace with the selected pattern
            var findDialog = new FindReplaceDialog(PassedTextControl);
            findDialog.XamlRoot = this.XamlRoot;
            // TODO: Pre-fill with dialog.SelectedPattern.Pattern and enable regex mode
            await findDialog.ShowAsync();
        }
    }

    private async void WebSearch_Click(object sender, RoutedEventArgs e)
    {
        string selected = PassedTextControl.SelectionLength > 0
            ? PassedTextControl.SelectedText
            : "";
        if (string.IsNullOrWhiteSpace(selected)) return;

        var settings = ((App)Application.Current).Host?.Services.GetService<IOptions<AppSettings>>();
        string url = settings?.Value?.WebSearchUrl ?? "https://www.google.com/search?q=";
        await Windows.System.Launcher.LaunchUriAsync(new Uri(url + Uri.EscapeDataString(selected)));
    }

    // --- Navigation ---

    private async void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (Navigator is { } nav)
            await nav.NavigateViewModelAsync<SettingsModel>(this);
    }

    private async void NavigateGrabFrame_Click(object sender, RoutedEventArgs e)
    {
        if (Navigator is { } nav)
            await nav.NavigateRouteAsync(this, "GrabFrame");
    }

    private async void NavigateQuickLookup_Click(object sender, RoutedEventArgs e)
    {
        if (Navigator is { } nav)
            await nav.NavigateRouteAsync(this, "QuickLookup");
    }

    private async void FindAndReplace_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new FindReplaceDialog(PassedTextControl);
        dialog.XamlRoot = this.XamlRoot;
        await dialog.ShowAsync();
    }

    // --- Format ---

    private async void Font_Click(object sender, RoutedEventArgs e)
    {
        // Simple font size/family picker
        var sizeBox = new TextBox { PlaceholderText = "Font size", Text = PassedTextControl.FontSize.ToString() };
        var familyBox = new TextBox { PlaceholderText = "Font family", Text = PassedTextControl.FontFamily.Source };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "Font Family:" });
        panel.Children.Add(familyBox);
        panel.Children.Add(new TextBlock { Text = "Font Size:" });
        panel.Children.Add(sizeBox);

        var dialog = new ContentDialog
        {
            Title = "Font Settings",
            Content = panel,
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        if (await dialog.ShowAsync() == ContentDialogResult.Primary)
        {
            if (double.TryParse(sizeBox.Text, out double size) && size > 0)
                PassedTextControl.FontSize = size;
            if (!string.IsNullOrWhiteSpace(familyBox.Text))
                PassedTextControl.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(familyBox.Text);
        }
    }

    private async void BottomBarSettings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Dialogs.BottomBarSettingsDialog { XamlRoot = this.XamlRoot };
        await dialog.ShowAsync();
    }

    // --- Help ---

    private void About_Click(object sender, RoutedEventArgs e)
    {
        StatusBarText.Text = "Text Grab v5.0 — Uno Platform Edition";
    }

    private async void Contact_Click(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("mailto:joe@JoeFinApps.com"));
    }

    private async void Feedback_Click(object sender, RoutedEventArgs e)
    {
        await Windows.System.Launcher.LaunchUriAsync(new Uri("https://github.com/TheJoeFin/Text-Grab/issues"));
    }

    // --- OCR ---

    private async void OcrFromImage_Click(object sender, RoutedEventArgs e)
    {
        var ocrService = this.FindServiceProvider()?.GetService(typeof(IOcrService)) as IOcrService;
        var fileService = this.FindServiceProvider()?.GetService(typeof(IFileService)) as IFileService;
        if (ocrService is null || fileService is null) return;

        StatusBarText.Text = "Picking image...";

        var imageData = await fileService.PickImageFileAsync();
        if (imageData is null || imageData.Length == 0)
        {
            StatusBarText.Text = "Ready";
            return;
        }

        StatusBarText.Text = "Running OCR...";

        using var stream = new MemoryStream(imageData);
        var result = await ocrService.RecognizeAsync(stream);

        if (result is null)
        {
            StatusBarText.Text = "OCR returned no results";
            return;
        }

        string ocrText = !string.IsNullOrWhiteSpace(result.CleanedOutput)
            ? result.CleanedOutput
            : result.RawOutput;

        if (string.IsNullOrEmpty(PassedTextControl.Text))
            PassedTextControl.Text = ocrText;
        else
            PassedTextControl.Text += Environment.NewLine + ocrText;

        StatusBarText.Text = $"OCR complete ({result.Engine})";
    }

    private async void OcrFromClipboard_Click(object sender, RoutedEventArgs e)
    {
        var ocrService = this.FindServiceProvider()?.GetService(typeof(IOcrService)) as IOcrService;
        if (ocrService is null) return;

        StatusBarText.Text = "Reading clipboard image...";

        try
        {
            var dataPackage = Clipboard.GetContent();
            if (!dataPackage.Contains(StandardDataFormats.Bitmap))
            {
                StatusBarText.Text = "No image in clipboard";
                return;
            }

            var streamRef = await dataPackage.GetBitmapAsync();
            using var randomStream = await streamRef.OpenReadAsync();
            using var memStream = new MemoryStream();
            await randomStream.AsStreamForRead().CopyToAsync(memStream);
            memStream.Position = 0;

            var result = await ocrService.RecognizeAsync(memStream);

            if (result is null)
            {
                StatusBarText.Text = "OCR returned no results";
                return;
            }

            string ocrText = !string.IsNullOrWhiteSpace(result.CleanedOutput)
                ? result.CleanedOutput
                : result.RawOutput;

            if (string.IsNullOrEmpty(PassedTextControl.Text))
                PassedTextControl.Text = ocrText;
            else
                PassedTextControl.Text += Environment.NewLine + ocrText;

            StatusBarText.Text = $"OCR complete ({result.Engine})";
        }
        catch (Exception ex)
        {
            StatusBarText.Text = $"OCR failed: {ex.Message}";
        }
    }

    // --- File I/O ---

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        if (_fileService is null) return;
        var content = await _fileService.PickAndReadTextFileAsync();
        if (content is null) return;

        PassedTextControl.Text = content;
        _openedFilePath = null; // FilePicker doesn't expose path on all platforms
        StatusBarText.Text = "File opened";
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_fileService is null) return;
        if (string.IsNullOrEmpty(_openedFilePath))
        {
            SaveAs_Click(sender, e);
            return;
        }

        try
        {
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(_openedFilePath);
            await Windows.Storage.FileIO.WriteTextAsync(file, PassedTextControl.Text);
            StatusBarText.Text = "Saved";
        }
        catch
        {
            SaveAs_Click(sender, e);
        }
    }

    private async void SaveAs_Click(object sender, RoutedEventArgs e)
    {
        if (_fileService is null) return;
        var saved = await _fileService.SaveTextFileAsync(PassedTextControl.Text);
        if (saved)
            StatusBarText.Text = "Saved";
    }

    // --- Drag and Drop ---

    private void PassedTextControl_DragOver(object sender, DragEventArgs e)
    {
        e.AcceptedOperation = DataPackageOperation.Copy;
    }

    private async void PassedTextControl_Drop(object sender, DragEventArgs e)
    {
        if (e.DataView.Contains(StandardDataFormats.Text))
        {
            var text = await e.DataView.GetTextAsync();
            ReplaceSelection(text);
        }
        else if (e.DataView.Contains(StandardDataFormats.StorageItems))
        {
            var items = await e.DataView.GetStorageItemsAsync();
            foreach (var item in items)
            {
                if (item is Windows.Storage.StorageFile file)
                {
                    var content = await Windows.Storage.FileIO.ReadTextAsync(file);
                    PassedTextControl.Text += Environment.NewLine + content;
                    _openedFilePath = file.Path;
                    StatusBarText.Text = $"Opened: {file.Name}";
                }
            }
        }
    }

    // --- Helpers ---

    private void ReplaceSelection(string replacement)
    {
        int start = PassedTextControl.SelectionStart;
        int length = PassedTextControl.SelectionLength;
        string text = PassedTextControl.Text;

        PassedTextControl.Text = string.Concat(
            text.AsSpan(0, start),
            replacement,
            text.AsSpan(start + length));

        PassedTextControl.Select(start + replacement.Length, 0);
    }

    private void ApplyToSelectionOrAll(Func<string, string> transform)
    {
        if (PassedTextControl.SelectionLength > 0)
        {
            string transformed = transform(PassedTextControl.SelectedText);
            ReplaceSelection(transformed);
        }
        else
        {
            PassedTextControl.Text = transform(PassedTextControl.Text);
        }
    }

    private int GetCurrentColumn()
    {
        var text = PassedTextControl.Text;
        int pos = PassedTextControl.SelectionStart;
        int col = 0;
        for (int i = pos - 1; i >= 0 && i < text.Length && text[i] != '\n'; i--)
            col++;
        return col;
    }

    private void MoveCurrentLine(int direction)
    {
        var text = PassedTextControl.Text;
        int pos = PassedTextControl.SelectionStart;
        if (string.IsNullOrEmpty(text)) return;

        var lines = text.Split('\n').ToList();

        // Find current line index
        int charCount = 0;
        int currentLineIndex = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            int lineLen = lines[i].Length + 1; // +1 for \n
            if (charCount + lineLen > pos)
            {
                currentLineIndex = i;
                break;
            }
            charCount += lineLen;
        }

        int targetIndex = currentLineIndex + direction;
        if (targetIndex < 0 || targetIndex >= lines.Count) return;

        // Swap lines
        (lines[currentLineIndex], lines[targetIndex]) = (lines[targetIndex], lines[currentLineIndex]);
        PassedTextControl.Text = string.Join('\n', lines);

        // Restore cursor to moved line
        int newPos = 0;
        for (int i = 0; i < targetIndex; i++)
            newPos += lines[i].Length + 1;
        PassedTextControl.Select(newPos, 0);
    }
}
