using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using Uno.Extensions.Configuration;

namespace TextGrab.Dialogs;

public sealed partial class RegexManagerDialog : ContentDialog
{
    private readonly ObservableCollection<StoredRegex> _patterns = [];
    private readonly IWritableOptions<AppSettings>? _settings;

    public StoredRegex? SelectedPattern { get; private set; }

    public RegexManagerDialog()
    {
        this.InitializeComponent();
        _settings = ((App)Application.Current).Host?.Services
            .GetService<IWritableOptions<AppSettings>>();
        LoadPatterns();
        PatternListView.ItemsSource = _patterns;
    }

    private void LoadPatterns()
    {
        _patterns.Clear();
        var settings = ((App)Application.Current).Host?.Services.GetService<IOptions<AppSettings>>();
        var json = settings?.Value?.RegexList;

        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var loaded = JsonSerializer.Deserialize<StoredRegex[]>(json);
                if (loaded is not null)
                    foreach (var p in loaded) _patterns.Add(p);
            }
            catch { }
        }

        if (_patterns.Count == 0)
        {
            foreach (var p in StoredRegex.GetDefaultPatterns())
                _patterns.Add(p);
            SavePatterns();
        }
    }

    private void SavePatterns()
    {
        var json = JsonSerializer.Serialize(_patterns.ToArray());
        _ = _settings?.UpdateAsync(s => s with { RegexList = json });
    }

    private void PatternListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        bool hasSelection = PatternListView.SelectedItem is not null;
        EditButton.IsEnabled = hasSelection;
        DeleteButton.IsEnabled = hasSelection;
        ExplainButton.IsEnabled = hasSelection;
        IsPrimaryButtonEnabled = hasSelection;
        TestPattern();
    }

    private async void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var (name, pattern, desc) = await ShowEditorAsync();
        if (name is null) return;

        var stored = new StoredRegex(name, pattern!, description: desc ?? "");
        _patterns.Add(stored);
        SavePatterns();
        PatternListView.SelectedItem = stored;
    }

    private async void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatternListView.SelectedItem is not StoredRegex selected) return;

        var (name, pattern, desc) = await ShowEditorAsync(selected);
        if (name is null) return;

        int index = _patterns.IndexOf(selected);
        if (index >= 0)
        {
            selected.Name = name;
            selected.Pattern = pattern!;
            selected.Description = desc ?? "";
            _patterns[index] = selected; // Force UI refresh
            SavePatterns();
        }
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatternListView.SelectedItem is not StoredRegex selected) return;

        var confirm = new ContentDialog
        {
            Title = "Delete Pattern",
            Content = $"Delete '{selected.Name}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        if (await confirm.ShowAsync() == ContentDialogResult.Primary)
        {
            _patterns.Remove(selected);
            SavePatterns();
        }
    }

    private async void ExplainButton_Click(object sender, RoutedEventArgs e)
    {
        if (PatternListView.SelectedItem is not StoredRegex selected) return;

        var explanation = StringMethods.ExplainRegexPattern(selected.Pattern);
        var dialog = new ContentDialog
        {
            Title = "Pattern Explanation",
            Content = explanation,
            CloseButtonText = "Close",
            XamlRoot = this.XamlRoot,
        };
        await dialog.ShowAsync();
    }

    private void UseButton_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        SelectedPattern = PatternListView.SelectedItem as StoredRegex;
        if (SelectedPattern is not null)
        {
            SelectedPattern.LastUsedDate = DateTimeOffset.Now;
            SavePatterns();
        }
    }

    private void TestTextBox_TextChanged(object sender, TextChangedEventArgs e) => TestPattern();

    private void TestPattern()
    {
        if (PatternListView.SelectedItem is not StoredRegex selected ||
            string.IsNullOrEmpty(TestTextBox?.Text))
        {
            if (MatchCountText is not null) MatchCountText.Text = "0 matches";
            return;
        }

        try
        {
            var matches = Regex.Matches(TestTextBox.Text, selected.Pattern, RegexOptions.Multiline);
            MatchCountText.Text = $"{matches.Count} match{(matches.Count != 1 ? "es" : "")}";
        }
        catch (ArgumentException)
        {
            MatchCountText.Text = "Invalid pattern";
        }
    }

    private async Task<(string? name, string? pattern, string? desc)> ShowEditorAsync(StoredRegex? existing = null)
    {
        var nameBox = new TextBox { PlaceholderText = "Pattern name", Text = existing?.Name ?? "" };
        var patternBox = new TextBox
        {
            PlaceholderText = "Regex pattern",
            Text = existing?.Pattern ?? "",
            AcceptsReturn = true,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Code, Consolas, monospace"),
            MinHeight = 60,
        };
        var descBox = new TextBox { PlaceholderText = "Description (optional)", Text = existing?.Description ?? "" };

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(nameBox);
        panel.Children.Add(patternBox);
        panel.Children.Add(descBox);

        var dialog = new ContentDialog
        {
            Title = existing is null ? "Add Pattern" : "Edit Pattern",
            Content = panel,
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary ||
            string.IsNullOrWhiteSpace(nameBox.Text) ||
            string.IsNullOrWhiteSpace(patternBox.Text))
            return (null, null, null);

        return (nameBox.Text, patternBox.Text, descBox.Text);
    }
}
