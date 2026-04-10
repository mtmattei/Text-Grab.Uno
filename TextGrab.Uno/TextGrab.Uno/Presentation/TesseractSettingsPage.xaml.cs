using Windows.ApplicationModel.DataTransfer;

namespace TextGrab.Presentation;

public sealed partial class TesseractSettingsPage : Page
{
    private bool _isLoading = true;

    public TesseractSettingsPage()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var model = GetModel();
        if (model is null) return;

        _isLoading = true;
        try
        {
            UseTesseractToggle.IsOn = await model.UseTesseract;
            TesseractPathTextBox.Text = await model.TesseractPath ?? "";

#if WINDOWS
            // Check if Tesseract can be found
            var path = TesseractPathTextBox.Text;
            if (!string.IsNullOrEmpty(path) && !System.IO.File.Exists(path))
            {
                CouldNotFindTessText.Visibility = Visibility.Visible;
            }
#endif
        }
        finally
        {
            _isLoading = false;
        }
    }

    private TesseractSettingsModel? GetModel() =>
        (DataContext as TesseractSettingsViewModel)?.Model;

    private void UseTesseractToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleUseTesseract();
    }

    private void TesseractPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.SetTesseractPath(TesseractPathTextBox.Text);

#if WINDOWS
        CouldNotFindTessText.Visibility =
            !string.IsNullOrEmpty(TesseractPathTextBox.Text) && !System.IO.File.Exists(TesseractPathTextBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
#endif
    }

    private void CopyWinGetButton_Click(object sender, RoutedEventArgs e)
    {
        ClipboardHelper.CopyText("winget install -e --id UB-Mannheim.TesseractOCR");
    }

    private void OpenPathButton_Click(object sender, RoutedEventArgs e)
    {
#if WINDOWS
        var path = TesseractPathTextBox.Text;
        if (!string.IsNullOrEmpty(path))
        {
            var dir = System.IO.Path.GetDirectoryName(path);
            if (dir is not null && System.IO.Directory.Exists(dir))
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
        }
#endif
    }
}
