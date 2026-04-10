using System.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;

namespace TextGrab.Controls;

public sealed partial class WordBorder : UserControl, INotifyPropertyChanged
{
    private SolidColorBrush _contrastingForeground = new(Colors.White);
    private SolidColorBrush _matchingBackground = new(Colors.Black);
    private DispatcherTimer _debounceTimer = new();
    private double _left;
    private double _top;

    public WordBorder()
    {
        InitializeComponent();
        _debounceTimer.Interval = TimeSpan.FromMilliseconds(300);
        _debounceTimer.Tick += DebounceTimer_Tick;
    }

    public WordBorder(WordBorderInfo info) : this()
    {
        Word = info.Word;
        Left = info.BorderRect.Left;
        Top = info.BorderRect.Top;
        Width = info.BorderRect.Width;
        Height = info.BorderRect.Height;
        LineNumber = info.LineNumber;
        ResultColumnID = info.ResultColumnID;
        ResultRowID = info.ResultRowID;
        IsBarcode = info.IsBarcode;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Callback interface for GrabFrame page interactions.</summary>
    public IGrabFrameHost? Host { get; set; }

    public string Word
    {
        get => EditWordTextBox.Text;
        set
        {
            EditWordTextBox.Text = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Word)));
        }
    }

    public double Left
    {
        get => _left;
        set
        {
            _left = value;
            Canvas.SetLeft(this, _left);
        }
    }

    public double Top
    {
        get => _top;
        set
        {
            _top = value;
            Canvas.SetTop(this, _top);
        }
    }

    public double Bottom => Top + Height;
    public double Right => Left + Width;
    public int LineNumber { get; set; }
    public int ResultColumnID { get; set; }
    public int ResultRowID { get; set; }
    public bool IsSelected { get; set; }
    public bool IsBarcode { get; set; }
    public bool IsEditing => EditWordTextBox.FocusState != FocusState.Unfocused;

    public SolidColorBrush MatchingBackground
    {
        get => _matchingBackground;
        set
        {
            _matchingBackground = value;
            MainGrid.Background = _matchingBackground;

            byte r = _matchingBackground.Color.R;
            byte g = _matchingBackground.Color.G;
            byte b = _matchingBackground.Color.B;
            double luma = 0.2126 * r + 0.7152 * g + 0.0722 * b;

            if (luma > 180)
            {
                _contrastingForeground = new SolidColorBrush(Colors.Black);
                EditWordTextBox.Foreground = _contrastingForeground;
            }
        }
    }

    private static readonly SolidColorBrush SelectedBrush = new(Colors.Orange);
    private static readonly SolidColorBrush DeselectedBrush = new(Windows.UI.Color.FromArgb(255, 48, 142, 152));

    public void Select()
    {
        IsSelected = true;
        WordBorderBorder.BorderBrush = SelectedBrush;
    }

    public void Deselect()
    {
        IsSelected = false;
        WordBorderBorder.BorderBrush = DeselectedBrush;
    }

    public void EnterEdit()
    {
        EditWordTextBox.Visibility = Visibility.Visible;
        MainGrid.Background = _matchingBackground;
    }

    public void ExitEdit()
    {
        EditWordTextBox.Visibility = Visibility.Collapsed;
        MainGrid.Background = new SolidColorBrush(_matchingBackground.Color) { Opacity = 0.1 };
    }

    public void FocusTextbox()
    {
        EditWordTextBox.Focus(FocusState.Programmatic);
        EditWordTextBox.SelectAll();
    }

    public bool IntersectsWith(Rect rectToCheck)
    {
        Rect wbRect = new(Left, Top, Width, Height);
        wbRect.Intersect(rectToCheck);
        return !wbRect.IsEmpty;
    }

    public WordBorderInfo ToInfo() => new()
    {
        Word = Word,
        BorderRect = new Rect(Left, Top, Width, Height),
        LineNumber = LineNumber,
        ResultColumnID = ResultColumnID,
        ResultRowID = ResultRowID,
        IsBarcode = IsBarcode,
        MatchingBackground = _matchingBackground.Color.ToString(),
    };

    // --- Event handlers ---

    private void WordBorder_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (Host?.IsCtrlDown == true)
            MoveResizeBorder.Visibility = Visibility.Visible;
        else
            MoveResizeBorder.Visibility = Visibility.Collapsed;
    }

    private void WordBorder_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        MoveResizeBorder.Visibility = Visibility.Collapsed;
    }

    private void EditWordTextBox_GotFocus(object sender, RoutedEventArgs e) => Select();

    private void EditWordTextBox_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Select();
        e.Handled = true;
    }

    private void EditWordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    private void DebounceTimer_Tick(object? sender, object e)
    {
        _debounceTimer.Stop();
        Host?.WordChanged();
    }

    private void CopyWordMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ClipboardHelper.CopyText(Word);
    }

    private void TryToNumberMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string oldWord = Word;
        Word = Word.TryFixToNumbers();
        Host?.UndoableWordChange(this, oldWord);
    }

    private void TryToAlphaMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string oldWord = Word;
        Word = Word.TryFixToLetters();
        Host?.UndoableWordChange(this, oldWord);
    }

    private void MakeSingleLineMenuItem_Click(object sender, RoutedEventArgs e)
    {
        string oldWord = Word;
        Word = Word.MakeStringSingleLine();
        Host?.UndoableWordChange(this, oldWord);
    }

    private void MergeWordBordersMenuItem_Click(object sender, RoutedEventArgs e)
        => Host?.MergeSelectedWordBorders();

    private void BreakIntoWordsMenuItem_Click(object sender, RoutedEventArgs e)
        => Host?.BreakWordBorderIntoWords(this);

    private void SearchForSimilarMenuItem_Click(object sender, RoutedEventArgs e)
        => Host?.SearchForSimilar(this);

    private void DeleteWordMenuItem_Click(object sender, RoutedEventArgs e)
        => Host?.DeleteWordBorder(this);

    private void MoveResizeBorder_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        Select();
        Host?.StartWordBorderMoveResize(this, Side.None);
        e.Handled = true;
    }

    private void SizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string tagStr)
            return;

        if (Enum.TryParse<Side>(tagStr, out var side))
            Host?.StartWordBorderMoveResize(this, side);

        e.Handled = true;
    }
}

/// <summary>
/// Interface for GrabFrame page to receive callbacks from WordBorder controls.
/// Replaces the WPF pattern of WordBorder holding a direct reference to GrabFrame.
/// </summary>
public interface IGrabFrameHost
{
    bool IsCtrlDown { get; }
    void WordChanged();
    void UndoableWordChange(WordBorder wb, string oldWord);
    void MergeSelectedWordBorders();
    void BreakWordBorderIntoWords(WordBorder wb);
    void SearchForSimilar(WordBorder wb);
    void DeleteWordBorder(WordBorder wb);
    void StartWordBorderMoveResize(WordBorder wb, Side side);
}
