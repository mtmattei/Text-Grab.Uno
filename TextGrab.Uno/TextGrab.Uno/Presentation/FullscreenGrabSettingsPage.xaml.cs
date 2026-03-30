namespace TextGrab.Presentation;

public sealed partial class FullscreenGrabSettingsPage : Page
{
    private bool _isLoading = true;

    public FullscreenGrabSettingsPage()
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
            // Start mode
            var mode = await model.FsgDefaultMode ?? "Default";
            for (int i = 0; i < StartModeRadioButtons.Items.Count; i++)
            {
                if (StartModeRadioButtons.Items[i] is RadioButton rb && rb.Tag?.ToString() == mode)
                {
                    StartModeRadioButtons.SelectedIndex = i;
                    break;
                }
            }

            SendToEtwToggle.IsOn = await model.FsgSendEtwToggle;
            ShadeOverlayToggle.IsOn = await model.FsgShadeOverlay;
            TryInsertToggle.IsOn = await model.TryInsert;

            var delay = await model.InsertDelay;
            InsertDelaySlider.Value = delay;
            InsertDelayValueText.Text = delay.ToString("F1");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private FullscreenGrabSettingsModel? GetModel() =>
        (DataContext as FullscreenGrabSettingsViewModel)?.Model;

    private void StartModeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        if (StartModeRadioButtons.SelectedItem is RadioButton rb && rb.Tag is string mode)
            _ = model.SetDefaultMode(mode);
    }

    private void SendToEtwToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleSendEtw();
    }

    private void ShadeOverlayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleShadeOverlay();
    }

    private void TryInsertToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading || GetModel() is not { } model) return;
        _ = model.ToggleTryInsert();
    }

    private void InsertDelaySlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (InsertDelayValueText is not null)
            InsertDelayValueText.Text = e.NewValue.ToString("F1");

        if (_isLoading || GetModel() is not { } model) return;
        _ = model.SetInsertDelay(e.NewValue);
    }
}
