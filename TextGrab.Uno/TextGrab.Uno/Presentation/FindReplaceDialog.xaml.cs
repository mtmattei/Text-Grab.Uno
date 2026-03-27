using System.Text.RegularExpressions;

namespace TextGrab.Presentation;

public sealed partial class FindReplaceDialog : ContentDialog
{
    private readonly TextBox _targetTextBox;
    private List<(int Index, int Length)> _matches = [];
    private int _currentMatchIndex = -1;

    public FindReplaceDialog(TextBox targetTextBox)
    {
        this.InitializeComponent();
        _targetTextBox = targetTextBox;
    }

    private void FindTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateMatches();
    }

    private void Option_Changed(object sender, RoutedEventArgs e)
    {
        UpdateMatches();
    }

    private void UpdateMatches()
    {
        _matches.Clear();
        _currentMatchIndex = -1;

        string searchText = FindTextBox.Text;
        string content = _targetTextBox.Text;

        if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(content))
        {
            MatchCountText.Text = "0 matches";
            return;
        }

        try
        {
            if (UseRegexCheckBox.IsChecked == true)
            {
                var options = RegexOptions.None;
                if (MatchCaseCheckBox.IsChecked != true)
                    options |= RegexOptions.IgnoreCase;

                var regex = new Regex(searchText, options, TimeSpan.FromSeconds(2));
                foreach (Match m in regex.Matches(content))
                    _matches.Add((m.Index, m.Length));
            }
            else
            {
                var comparison = MatchCaseCheckBox.IsChecked == true
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;

                int index = 0;
                while ((index = content.IndexOf(searchText, index, comparison)) >= 0)
                {
                    _matches.Add((index, searchText.Length));
                    index += searchText.Length;
                }
            }
        }
        catch (RegexParseException)
        {
            MatchCountText.Text = "Invalid regex";
            return;
        }

        MatchCountText.Text = $"{_matches.Count} match{(_matches.Count != 1 ? "es" : "")}";
    }

    private void FindNext_Click(object sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0) return;

        _currentMatchIndex = (_currentMatchIndex + 1) % _matches.Count;
        var (index, length) = _matches[_currentMatchIndex];
        _targetTextBox.Select(index, length);
        _targetTextBox.Focus(FocusState.Programmatic);

        MatchCountText.Text = $"Match {_currentMatchIndex + 1} of {_matches.Count}";
    }

    private void Replace_Click(object sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0 || _currentMatchIndex < 0) return;

        var (index, length) = _matches[_currentMatchIndex];
        string text = _targetTextBox.Text;
        string replacement = ReplaceTextBox.Text ?? string.Empty;

        _targetTextBox.Text = string.Concat(
            text.AsSpan(0, index),
            replacement,
            text.AsSpan(index + length));

        _targetTextBox.Select(index + replacement.Length, 0);
        UpdateMatches();

        // Advance to next match if available
        if (_matches.Count > 0)
        {
            _currentMatchIndex = Math.Min(_currentMatchIndex, _matches.Count - 1);
            var next = _matches[_currentMatchIndex];
            _targetTextBox.Select(next.Index, next.Length);
        }
    }

    private void ReplaceAll_Click(object sender, RoutedEventArgs e)
    {
        if (_matches.Count == 0) return;

        string text = _targetTextBox.Text;
        string replacement = ReplaceTextBox.Text ?? string.Empty;

        // Replace backwards to preserve indices
        for (int i = _matches.Count - 1; i >= 0; i--)
        {
            var (index, length) = _matches[i];
            text = string.Concat(
                text.AsSpan(0, index),
                replacement,
                text.AsSpan(index + length));
        }

        int count = _matches.Count;
        _targetTextBox.Text = text;
        UpdateMatches();

        MatchCountText.Text = $"Replaced {count} occurrence{(count != 1 ? "s" : "")}";
    }
}
