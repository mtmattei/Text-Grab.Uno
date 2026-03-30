namespace TextGrab.Controls;

public sealed partial class CollapsibleButton : UserControl
{
    public static readonly DependencyProperty ButtonTextProperty =
        DependencyProperty.Register(nameof(ButtonText), typeof(string), typeof(CollapsibleButton), new PropertyMetadata("Button"));

    public static readonly DependencyProperty ButtonGlyphProperty =
        DependencyProperty.Register(nameof(ButtonGlyph), typeof(string), typeof(CollapsibleButton), new PropertyMetadata("\uE71E")); // Diamond

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(CollapsibleButton), new PropertyMetadata(false, OnIsCompactChanged));

    public static readonly DependencyProperty CanChangeStyleProperty =
        DependencyProperty.Register(nameof(CanChangeStyle), typeof(bool), typeof(CollapsibleButton), new PropertyMetadata(true));

    public CollapsibleButton()
    {
        this.InitializeComponent();
    }

    public event RoutedEventHandler? Click;

    public string ButtonText
    {
        get => (string)GetValue(ButtonTextProperty);
        set => SetValue(ButtonTextProperty, value);
    }

    public string ButtonGlyph
    {
        get => (string)GetValue(ButtonGlyphProperty);
        set => SetValue(ButtonGlyphProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    public bool CanChangeStyle
    {
        get => (bool)GetValue(CanChangeStyleProperty);
        set => SetValue(CanChangeStyleProperty, value);
    }

    private void InnerButton_Click(object sender, RoutedEventArgs e)
    {
        Click?.Invoke(this, e);
    }

    private void ChangeStyleItem_Click(object sender, RoutedEventArgs e)
    {
        if (!CanChangeStyle)
            return;

        IsCompact = !IsCompact;
    }

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CollapsibleButton button)
        {
            button.UpdateLayout((bool)e.NewValue);
        }
    }

    private void UpdateLayout(bool compact)
    {
        if (ButtonTextBlock is null)
            return;

        ButtonTextBlock.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
    }
}
