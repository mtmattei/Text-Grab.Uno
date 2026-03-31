using System.Text;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;

namespace TextGrab.Presentation;

public sealed partial class QuickLookupPage : Page
{
    private List<LookupItem> _masterItems = [];
    private DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private bool _isRegex;

    public QuickLookupPage()
    {
        InitializeComponent();
        _searchTimer.Tick += SearchTimer_Tick;
    }

    // --- Data loading ---

    private async void OpenCSV_Click(object sender, RoutedEventArgs e)
    {
        var fileService = GetService<IFileService>();
        if (fileService is null) return;

        var content = await fileService.PickAndReadTextFileAsync();
        if (string.IsNullOrEmpty(content)) return;

        _masterItems = content.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => LookupItem.ParseCSVLine(l.TrimEnd('\r')))
            .ToList();

        RefreshList(_masterItems);
        StatusBarText.Text = $"{_masterItems.Count} items loaded from CSV";
    }

    private async void PasteData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dataPackage = Clipboard.GetContent();
            if (!dataPackage.Contains(StandardDataFormats.Text)) return;

            var text = await dataPackage.GetTextAsync();
            if (string.IsNullOrWhiteSpace(text)) return;

            bool isTab = text.Contains('\t');
            var newItems = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => isTab
                    ? LookupItem.ParseTabLine(l.TrimEnd('\r'))
                    : LookupItem.ParseCSVLine(l.TrimEnd('\r')))
                .ToList();

            _masterItems.AddRange(newItems);
            RefreshList(_masterItems);
            SaveButton.Visibility = Visibility.Visible;
            StatusBarText.Text = $"{newItems.Count} items added from clipboard";
        }
        catch (Exception ex)
        {
            StatusBarText.Text = $"Paste failed: {ex.Message}";
        }
    }

    private async void SaveCSV_Click(object sender, RoutedEventArgs e)
    {
        var fileService = GetService<IFileService>();
        if (fileService is null) return;

        var sb = new StringBuilder();
        foreach (var item in _masterItems)
        {
            if (item.HistoryItem is not null) continue;
            sb.AppendLine(item.ToCSVString());
        }

        var saved = await fileService.SaveTextFileAsync(sb.ToString(), "lookup.csv");
        if (saved)
        {
            SaveButton.Visibility = Visibility.Collapsed;
            StatusBarText.Text = "Saved";
        }
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

    private void RegExToggle_Click(object sender, RoutedEventArgs e)
    {
        _isRegex = RegExToggleButton.IsChecked == true;
        ApplySearch();
    }

    private void ApplySearch()
    {
        string searchText = SearchBox.Text;

        if (string.IsNullOrEmpty(searchText))
        {
            RefreshList(_masterItems);
            return;
        }

        List<LookupItem> filtered;

        if (_isRegex)
        {
            try
            {
                var regex = new Regex(searchText, RegexOptions.IgnoreCase);
                filtered = _masterItems.Where(item => regex.IsMatch(item.ToString())).ToList();
                RegExToggleButton.BorderBrush = null;
            }
            catch (RegexParseException)
            {
                filtered = StandardSearch(searchText);
                RegExToggleButton.BorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red);
            }
        }
        else
        {
            filtered = StandardSearch(searchText);
        }

        RefreshList(filtered);
    }

    private List<LookupItem> StandardSearch(string searchText)
    {
        string[] words = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return _masterItems.Where(item =>
        {
            string itemText = item.ToString();
            string firstLetters = item.FirstLettersString;

            return words.All(word =>
                itemText.Contains(word, StringComparison.OrdinalIgnoreCase)
                || firstLetters.Contains(word, StringComparison.OrdinalIgnoreCase));
        }).ToList();
    }

    // --- Selection / Copy ---

    private void CopySelected_Click(object sender, RoutedEventArgs e) => CopySelection();

    private void ItemsListView_ItemClick(object sender, ItemClickEventArgs e) { }

    private void ItemsListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        => CopySelection();

    private void CopySelection(bool copyShort = false, bool copyBoth = false)
    {
        var selected = ItemsListView.SelectedItems.OfType<LookupItem>().ToList();
        if (selected.Count == 0 && ItemsListView.Items.Count > 0)
        {
            if (ItemsListView.Items[0] is LookupItem first)
                selected = [first];
        }

        if (selected.Count == 0) return;

        StringBuilder sb = new();
        foreach (var item in selected)
        {
            if (sb.Length > 0) sb.AppendLine();

            if (copyBoth)
                sb.Append(item.ToString());
            else if (copyShort)
                sb.Append(item.ShortValue);
            else
                sb.Append(item.LongValue);
        }

        var dp = new DataPackage();
        dp.SetText(sb.ToString());
        Clipboard.SetContent(dp);
        StatusBarText.Text = $"Copied {selected.Count} item{(selected.Count != 1 ? "s" : "")}";
    }

    // --- Context menu ---

    private void CopyValue_Click(object sender, RoutedEventArgs e)
    {
        if (GetContextItem(sender) is LookupItem item)
            CopyToClipboard(item.LongValue);
    }

    private void CopyKey_Click(object sender, RoutedEventArgs e)
    {
        if (GetContextItem(sender) is LookupItem item)
            CopyToClipboard(item.ShortValue);
    }

    private void CopyBoth_Click(object sender, RoutedEventArgs e)
    {
        if (GetContextItem(sender) is LookupItem item)
            CopyToClipboard(item.ToString());
    }

    private void DeleteItem_Click(object sender, RoutedEventArgs e)
    {
        if (GetContextItem(sender) is LookupItem item)
        {
            _masterItems.Remove(item);
            ApplySearch();
            SaveButton.Visibility = Visibility.Visible;
            StatusBarText.Text = "Item deleted";
        }
    }

    private void AddRow_Click(object sender, RoutedEventArgs e)
    {
        var newItem = new LookupItem { ShortValue = "New Key", LongValue = "New Value" };
        _masterItems.Add(newItem);
        ApplySearch();
        SaveButton.Visibility = Visibility.Visible;
        StatusBarText.Text = "Row added";
    }

    // --- Keyboard ---

    private void Page_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        bool ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        switch (e.Key)
        {
            case Windows.System.VirtualKey.Enter:
                CopySelection();
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.Escape:
                if (!string.IsNullOrEmpty(SearchBox.Text))
                {
                    SearchBox.Text = string.Empty;
                    e.Handled = true;
                }
                break;

            case Windows.System.VirtualKey.Delete:
                DeleteSelectedItems();
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.S when ctrl:
                SaveCSV_Click(sender, new RoutedEventArgs());
                e.Handled = true;
                break;

            case Windows.System.VirtualKey.R when ctrl:
                RegExToggleButton.IsChecked = !(RegExToggleButton.IsChecked ?? false);
                _isRegex = RegExToggleButton.IsChecked == true;
                ApplySearch();
                e.Handled = true;
                break;
        }
    }

    private void DeleteSelectedItems()
    {
        var selected = ItemsListView.SelectedItems.OfType<LookupItem>().ToList();
        foreach (var item in selected)
            _masterItems.Remove(item);

        if (selected.Count > 0)
        {
            ApplySearch();
            SaveButton.Visibility = Visibility.Visible;
            StatusBarText.Text = $"{selected.Count} item{(selected.Count != 1 ? "s" : "")} deleted";
        }
    }

    // --- Helpers ---

    private void RefreshList(List<LookupItem> items)
    {
        ItemsListView.ItemsSource = items;
        ItemCountText.Text = $"{items.Count} item{(items.Count != 1 ? "s" : "")}";
        EmptyState.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static LookupItem? GetContextItem(object sender)
    {
        if (sender is FrameworkElement fe && fe.DataContext is LookupItem item)
            return item;
        return null;
    }

    private static void CopyToClipboard(string text)
    {
        var dp = new DataPackage();
        dp.SetText(text);
        Clipboard.SetContent(dp);
    }

    private T? GetService<T>() where T : class
        => this.FindServiceProvider()?.GetService(typeof(T)) as T;
}
