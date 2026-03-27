using System;
using System.Linq;

namespace TextGrab.Models;

public enum LookupItemKind
{
    Simple = 0,
    EditWindow = 1,
    GrabFrame = 2,
    Link = 3,
    Command = 4,
    Dynamic = 5,
}

public class LookupItem : IEquatable<LookupItem>
{
    public string ShortValue { get; set; } = string.Empty;
    public string LongValue { get; set; } = string.Empty;

    public string UiSymbol
    {
        get
        {
            return Kind switch
            {
                LookupItemKind.Simple => "Copy20",
                LookupItemKind.EditWindow => "Window24",
                LookupItemKind.GrabFrame => "PanelBottom20",
                LookupItemKind.Link => "Link24",
                LookupItemKind.Command => "WindowConsole20",
                LookupItemKind.Dynamic => "Flash24",
                _ => "Copy20",
            };
        }
    }

    public LookupItemKind Kind { get; set; } = LookupItemKind.Simple;

    public LookupItem()
    {

    }

    public string FirstLettersString => string.Join("", ShortValue.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[0])).ToLower();

    public LookupItem(string sv, string lv)
    {
        ShortValue = sv;
        LongValue = lv;
    }

    public LookupItem(HistoryInfo historyInfo)
    {
        ShortValue = historyInfo.CaptureDateTime.ToString("F");
        LongValue = historyInfo.TextContent.Length > 100 ? historyInfo.TextContent[..100].Trim() + "..." : historyInfo.TextContent.Trim();

        HistoryItem = historyInfo;

        if (string.IsNullOrEmpty(historyInfo.ImagePath))
            Kind = LookupItemKind.EditWindow;
        else
            Kind = LookupItemKind.GrabFrame;
    }

    public HistoryInfo? HistoryItem { get; set; }

    public override string ToString()
    {
        if (HistoryItem is not null)
            return $"{HistoryItem.CaptureDateTime:F} {HistoryItem.TextContent}";

        return $"{ShortValue} {LongValue}";
    }

    public string ToCSVString() => $"{ShortValue},{LongValue}";

    public bool Equals(LookupItem? other)
    {
        if (other is null)
            return false;

        if (other.ToString() == ToString())
            return true;

        return false;
    }
}
