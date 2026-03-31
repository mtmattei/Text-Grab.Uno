using System.Collections.ObjectModel;

namespace TextGrab.Dialogs;

public sealed partial class PostGrabActionEditorDialog : ContentDialog
{
    private readonly ObservableCollection<ButtonInfo> _available = [];
    private readonly ObservableCollection<ButtonInfo> _enabled = [];

    private static readonly string[] DefaultActions =
    [
        "Fix GUIDs",
        "Trim Each Line",
        "Remove Duplicate Lines",
        "Try To Numbers",
        "Try To Letters",
    ];

    public PostGrabActionEditorDialog()
    {
        this.InitializeComponent();
        AvailableListView.ItemsSource = _available;
        EnabledListView.ItemsSource = _enabled;
        LoadDefaults();
    }

    private void LoadDefaults()
    {
        _enabled.Clear();
        _available.Clear();

        foreach (var name in DefaultActions)
            _enabled.Add(new ButtonInfo { ButtonText = name });

        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        EmptyStateText.Visibility = _available.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (AvailableListView.SelectedItem is ButtonInfo item)
        {
            _available.Remove(item);
            _enabled.Add(item);
            UpdateEmptyState();
        }
    }

    private void RemoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (EnabledListView.SelectedItem is ButtonInfo item)
        {
            _enabled.Remove(item);
            _available.Add(item);
            UpdateEmptyState();
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

    private void SaveButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // TODO: Persist enabled actions order to settings
    }

    private void ResetButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true; // Don't close dialog
        LoadDefaults();
    }
}
