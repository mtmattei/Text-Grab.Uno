using Windows.Foundation;

namespace TextGrab.Models;

public class WordBorderInfo
{
    public string Word { get; set; } = string.Empty;
    public Rect BorderRect { get; set; } = Rect.Empty;
    public int LineNumber { get; set; } = 0;
    public int ResultColumnID { get; set; } = 0;
    public int ResultRowID { get; set; } = 0;
    public string MatchingBackground { get; set; } = "Transparent";
    public bool IsBarcode { get; set; } = false;
}
