using System.Collections.ObjectModel;
using Uno.Extensions.Configuration;

namespace TextGrab.Dialogs;

public sealed partial class BottomBarSettingsDialog : ContentDialog
{
    private readonly ObservableCollection<ButtonInfo> _available = [];
    private readonly ObservableCollection<ButtonInfo> _enabled = [];

    public BottomBarSettingsDialog()
    {
        this.InitializeComponent();
        AvailableListView.ItemsSource = _available;
        EnabledListView.ItemsSource = _enabled;
        LoadSettings();
    }

    private void LoadSettings()
    {
        var settings = this.GetService<IOptions<AppSettings>>();
        if (settings?.Value is null) return;

        ShowCursorTextToggle.IsOn = settings.Value.ShowCursorText;
        ShowWordCountToggle.IsOn = settings.Value.EtwShowWordCount;
        ShowCharDetailsToggle.IsOn = settings.Value.EtwShowCharDetails;
        ShowLangPickerToggle.IsOn = settings.Value.EtwShowLangPicker;

        // Initialize with some default button items
        _available.Add(new ButtonInfo { ButtonText = "Make Single Line" });
        _available.Add(new ButtonInfo { ButtonText = "Trim Each Line" });
        _available.Add(new ButtonInfo { ButtonText = "Remove Duplicates" });
        _available.Add(new ButtonInfo { ButtonText = "Toggle Case" });
        _available.Add(new ButtonInfo { ButtonText = "Replace Reserved" });
        _available.Add(new ButtonInfo { ButtonText = "Try Numbers" });
        _available.Add(new ButtonInfo { ButtonText = "Try Letters" });
        _available.Add(new ButtonInfo { ButtonText = "Fix GUIDs" });
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (AvailableListView.SelectedItem is ButtonInfo item)
        {
            _available.Remove(item);
            _enabled.Add(item);
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (EnabledListView.SelectedItem is ButtonInfo item)
        {
            _enabled.Remove(item);
            _available.Add(item);
        }
    }

    private void MoveUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (EnabledListView.SelectedItem is ButtonInfo item)
        {
            int index = _enabled.IndexOf(item);
            if (index > 0)
            {
                _enabled.Move(index, index - 1);
                EnabledListView.SelectedItem = item;
            }
        }
    }

    private void MoveDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (EnabledListView.SelectedItem is ButtonInfo item)
        {
            int index = _enabled.IndexOf(item);
            if (index < _enabled.Count - 1)
            {
                _enabled.Move(index, index + 1);
                EnabledListView.SelectedItem = item;
            }
        }
    }

    private async void SaveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        var writableSettings = this.GetService<IWritableOptions<AppSettings>>();
        if (writableSettings is null) return;

        await writableSettings.UpdateAsync(s => s with
        {
            ShowCursorText = ShowCursorTextToggle.IsOn,
            EtwShowWordCount = ShowWordCountToggle.IsOn,
            EtwShowCharDetails = ShowCharDetailsToggle.IsOn,
            EtwShowLangPicker = ShowLangPickerToggle.IsOn,
        });
    }
}
