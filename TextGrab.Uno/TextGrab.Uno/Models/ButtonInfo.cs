using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TextGrab.Models;

public enum DefaultCheckState
{
    Off = 0,
    LastUsed = 1,
    On = 2
}

public class ButtonInfo
{
    public double OrderNumber { get; set; } = 0.1;
    public string ButtonText { get; set; } = "";
    public string SymbolText { get; set; } = "";
    public string Background { get; set; } = "Transparent";
    public string Command { get; set; } = "";
    public string ClickEvent { get; set; } = "";
    public bool IsSymbol { get; set; } = false;

    [JsonIgnore]
    public string SymbolIcon { get; set; } = "Diamond24";

    public bool IsRelevantForFullscreenGrab { get; set; } = false;
    public bool IsRelevantForEditWindow { get; set; } = true;
    public DefaultCheckState DefaultCheckState { get; set; } = DefaultCheckState.Off;

    public ButtonInfo()
    {

    }

    public override bool Equals(object? obj)
    {
        if (obj is not ButtonInfo otherButton)
            return false;

        return otherButton.GetHashCode() == GetHashCode();
    }

    public override int GetHashCode()
    {
        return System.HashCode.Combine(
            ButtonText,
            SymbolText,
            Background,
            Command,
            ClickEvent,
            IsRelevantForFullscreenGrab,
            IsRelevantForEditWindow,
            DefaultCheckState);
    }

    public ButtonInfo(string buttonText, string symbolText, string background, string command, string clickEvent, bool isSymbol)
    {
        ButtonText = buttonText;
        SymbolText = symbolText;
        Background = background;
        Command = command;
        ClickEvent = clickEvent;
        IsSymbol = isSymbol;
    }

    public ButtonInfo(string buttonText, string clickEvent, string symbolIcon, DefaultCheckState defaultCheckState)
    {
        ButtonText = buttonText;
        ClickEvent = clickEvent;
        SymbolIcon = symbolIcon;
        IsSymbol = true;
        IsRelevantForFullscreenGrab = true;
        IsRelevantForEditWindow = false;
        DefaultCheckState = defaultCheckState;
    }

    private static List<ButtonInfo>? _defaultButtonList;
    public static List<ButtonInfo> DefaultButtonList
    {
        get
        {
            if (_defaultButtonList is not null)
                return _defaultButtonList;

            _defaultButtonList =
            [
        new()
        {
            ButtonText = "Copy and Close",
            SymbolText = "",
            Background = "#CC7000",
            ClickEvent = "CopyCloseBTN_Click",
            SymbolIcon = "Copy24"
        },
        new()
        {
            ButtonText = "Save to File...",
            SymbolText = "",
            ClickEvent = "SaveBTN_Click",
            SymbolIcon = "Save24"
        },
        new()
        {
            ButtonText = "Make Single Line",
            SymbolText = "",
            Command = "SingleLineCmd",
            SymbolIcon = "SubtractSquare24"
        },
        new()
        {
            ButtonText = "New Fullscreen Grab",
            SymbolText = "",
            ClickEvent = "NewFullscreen_Click",
            IsSymbol = true,
            SymbolIcon = "SlideAdd24"
        },
        new()
        {
            ButtonText = "Open Grab Frame",
            SymbolText = "",
            ClickEvent = "OpenGrabFrame_Click",
            IsSymbol = true,
            SymbolIcon = "PanelBottom20"
        },
        new()
        {
            ButtonText = "Find and Replace",
            SymbolText = "",
            ClickEvent = "SearchButton_Click",
            IsSymbol = true,
            SymbolIcon = "Search24"
        },
        new()
        {
            ButtonText = "Edit Bottom Bar",
            SymbolText = "",
            ClickEvent = "EditBottomBarMenuItem_Click",
            IsSymbol = true,
                    SymbolIcon = "CalendarSettings24"
                },
                    ];

            return _defaultButtonList;
        }
    }

    private static List<ButtonInfo>? _allButtons;
    public static List<ButtonInfo> AllButtons
    {
        get
        {
            if (_allButtons is not null)
                return _allButtons;

            _allButtons =
            [
        new()
        {
            OrderNumber = 1.1,
            ButtonText = "Copy and Close",
            SymbolText = "",
            Background = "#CC7000",
            ClickEvent = "CopyCloseBTN_Click",
            SymbolIcon = "Copy24"
        },
        new()
        {
            OrderNumber = 1.11,
            ButtonText = "Close and Insert",
            SymbolText = "",
            Background = "#CC7000",
            ClickEvent = "CopyClosePasteBTN_Click",
            SymbolIcon = "ClipboardTaskAdd24"
        },
        new()
        {
            OrderNumber = 1.2,
            ButtonText = "Save to File...",
            SymbolText = "",
            ClickEvent = "SaveBTN_Click",
            SymbolIcon = "DocumentSave24"
        },
        new()
        {
            OrderNumber = 1.3,
            ButtonText = "Make Single Line",
            SymbolText = "",
            Command = "SingleLineCmd",
            SymbolIcon = "SubtractSquare24"
        },
        new()
        {
            OrderNumber = 1.4,
            ButtonText = "New Fullscreen Grab",
            SymbolText = "",
            ClickEvent = "NewFullscreen_Click",
            SymbolIcon = "SlideAdd24"
        },
        new()
        {
            OrderNumber = 1.41,
            ButtonText = "Fullscreen Grab With Delay",
            SymbolText = "",
            ClickEvent = "FSGDelayMenuItem_Click",
            SymbolIcon = "Timer324"
        },
        new()
        {
            OrderNumber = 1.5,
            ButtonText = "Open Grab Frame",
            SymbolText = "",
            ClickEvent = "OpenGrabFrame_Click",
            SymbolIcon = "PanelBottom20"
        },
        new()
        {
            OrderNumber = 1.6,
            ButtonText = "Find and Replace",
            SymbolText = "",
            ClickEvent = "SearchButton_Click",
            SymbolIcon = "Search24"
        },
        new()
        {
            OrderNumber = 1.61,
            ButtonText = "Regex Manager",
            SymbolText = "",
            ClickEvent = "RegexManagerMenuItem_Click",
            SymbolIcon = "Book24"
        },
        new()
        {
            OrderNumber = 1.7,
            ButtonText = "Web Search",
            SymbolText = "",
            Command = "DefaultWebSearchCmd",
            SymbolIcon = "GlobeSearch24"
        },
        new()
        {
            OrderNumber = 2.1,
            ButtonText = "Open Settings",
            SymbolText = "",
            ClickEvent = "SettingsMenuItem_Click",
            SymbolIcon = "Settings24"
        },
        new()
        {
            OrderNumber = 2.2,
            ButtonText = "Open File...",
            SymbolText = "",
            ClickEvent = "OpenFileMenuItem_Click",
            SymbolIcon = "DocumentArrowRight24"
        },
        new()
        {
            OrderNumber = 2.3,
            ButtonText = "OCR Paste",
            SymbolText = "",
            Command = "PasteCommand",
            SymbolIcon = "ClipboardImage24"
        },
        new()
        {
            OrderNumber = 2.4,
            ButtonText = "Launch URL",
            SymbolText = "",
            Command = "LaunchCmd",
            SymbolIcon = "Globe24"
        },
        new()
        {
            OrderNumber = 3.1,
            ButtonText = "Trim Each Line",
            SymbolText = "",
            ClickEvent = "TrimEachLineMenuItem_Click",
            SymbolIcon = "TextCollapse24"
        },
        new()
        {
            OrderNumber = 3.2,
            ButtonText = "Try to make Numbers",
            SymbolText = "",
            ClickEvent = "TryToNumberMenuItem_Click",
            SymbolIcon = "NumberRow24"
        },
        new()
        {
            OrderNumber = 3.3,
            ButtonText = "Try to make Letters",
            SymbolText = "",
            ClickEvent = "TryToAlphaMenuItem_Click",
            SymbolIcon = "TextT24"
        },
        new()
        {
            OrderNumber = 3.4,
            ButtonText = "Toggle Case",
            SymbolText = "",
            Command = "ToggleCaseCmd",
            SymbolIcon = "TextChangeCase24"
        },
        new()
        {
            OrderNumber = 3.5,
            ButtonText = "Remove Duplicate Lines",
            SymbolText = "",
            ClickEvent = "RemoveDuplicateLines_Click",
            SymbolIcon = "MultiselectLtr24"
        },
        new()
        {
            OrderNumber = 3.6,
            ButtonText = "Replace Reserved Characters",
            SymbolText = "",
            Command = "ReplaceReservedCmd",
            SymbolIcon = "RoadCone24"
        },
        new()
        {
            OrderNumber = 3.7,
            ButtonText = "Unstack Text (Select Top Row)",
            SymbolText = "",
            Command = "UnstackCmd",
            SymbolIcon = "TableStackAbove24"
        },
        new()
        {
            OrderNumber = 3.8,
            ButtonText = "Unstack Text (Select First Column)",
            SymbolText = "",
            Command = "UnstackGroupCmd",
            SymbolIcon = "TableStackLeft24"
        },
        new()
        {
            OrderNumber = 3.9,
            ButtonText = "Add or Remove at...",
            SymbolText = "",
            ClickEvent = "AddRemoveAtMenuItem_Click",
            SymbolIcon = "ArrowSwap24"
        },
        new()
        {
            OrderNumber = 4.1,
            ButtonText = "Select Word",
            SymbolText = "",
            ClickEvent = "SelectWordMenuItem_Click",
            SymbolIcon = "Highlight24"
        },
        new()
        {
            OrderNumber = 4.2,
            ButtonText = "Select Line",
            SymbolText = "",
            ClickEvent = "SelectLineMenuItem_Click",
            SymbolIcon = "ArrowFit20"
        },
        new()
        {
            OrderNumber = 4.3,
            ButtonText = "Move Line Up",
            SymbolText = "",
            ClickEvent = "MoveLineUpMenuItem_Click",
            SymbolIcon = "ArrowUpload24"
        },
        new()
        {
            OrderNumber = 4.4,
            ButtonText = "Move Line Down",
            SymbolText = "",
            ClickEvent = "MoveLineDownMenuItem_Click",
            SymbolIcon = "ArrowDownload24"
        },
        new()
        {
            OrderNumber = 4.5,
            ButtonText = "Split on Selection",
            SymbolText = "",
            Command = "SplitOnSelectionCmd",
            SymbolIcon = "TextWrap24"
        },
        new()
        {
            OrderNumber = 4.6,
            ButtonText = "Isolate Selection",
            SymbolText = "",
            Command = "IsolateSelectionCmd",
            SymbolIcon = "ShapeExclude24"
        },
        new()
        {
            OrderNumber = 4.7,
            ButtonText = "Delete All of Selection",
            SymbolText = "",
            Command = "DeleteAllSelectionCmd",
            SymbolIcon = "Delete24"
        },
        new()
        {
            OrderNumber = 4.8,
            ButtonText = "Delete All of Pattern",
            SymbolText = "",
            Command = "DeleteAllSelectionPatternCmd",
            SymbolIcon = "DeleteLines20"
        },
        new()
        {
            OrderNumber = 4.9,
            ButtonText = "Insert on Every Line",
            SymbolText = "",
            Command = "InsertSelectionOnEveryLineCmd",
            SymbolIcon = "TextIndentIncreaseLtr24"
        },
        new()
        {
            OrderNumber = 5.1,
            ButtonText = "New Quick Simple Lookup",
            SymbolText = "",
            ClickEvent = "LaunchQuickSimpleLookup",
            SymbolIcon = "SlideSearch24"
        },
        new()
        {
            OrderNumber = 5.2,
            ButtonText = "List Files and Folders...",
            SymbolText = "",
            ClickEvent = "ListFilesMenuItem_Click",
            SymbolIcon = "DocumentBulletListMultiple24"
        },
        new()
        {
            OrderNumber = 5.3,
            ButtonText = "Extract Text from Images...",
            SymbolText = "",
            ClickEvent = "ReadFolderOfImages_Click",
            SymbolIcon = "ImageMultiple24"
        },
        new()
        {
            OrderNumber = 5.4,
            ButtonText = "Extract Text from Images to txt Files...",
            SymbolText = "",
            ClickEvent = "ReadFolderOfImagesWriteTxtFiles_Click",
            SymbolIcon = "TabDesktopImage24"
        },
        new()
        {
            OrderNumber = 5.5,
            ButtonText = "New Window",
            SymbolText = "",
            ClickEvent = "NewWindow_Clicked",
            SymbolIcon = "WindowNew24"
        },
        new()
        {
            OrderNumber = 5.6,
            ButtonText = "New Window from Selection",
            SymbolText = "",
            ClickEvent = "NewWindowWithText_Clicked",
            SymbolIcon = "WindowLocationTarget20"
        },
        new()
        {
            OrderNumber = 5.7,
            ButtonText = "Make QR Code",
            SymbolText = "",
            Command = "MakeQrCodeCmd",
            SymbolIcon = "QrCode24"
        },
        new()
        {
            ButtonText = "Edit Bottom Bar",
            ClickEvent = "EditBottomBarMenuItem_Click",
            SymbolIcon = "CalendarEdit24"
        },
        new()
        {
                        ButtonText = "Settings",
                        ClickEvent = "SettingsMenuItem_Click",
                        SymbolIcon = "Settings24"
                    },
                        ];

            return _allButtons;
        }
    }
}
