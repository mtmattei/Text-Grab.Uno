namespace TextGrab.Services;

/// <summary>
/// Manages capture and edit history persistence.
/// </summary>
public interface IHistoryService
{
    Task LoadHistoriesAsync(CancellationToken ct = default);
    Task WriteHistoryAsync(CancellationToken ct = default);
    IReadOnlyList<HistoryInfo> GetTextHistory();
    IReadOnlyList<HistoryInfo> GetImageHistory();
    Task SaveTextHistoryAsync(HistoryInfo item, CancellationToken ct = default);
    Task DeleteAllHistoryAsync(CancellationToken ct = default);
}

public partial record HistoryInfo(
    string Id,
    string TextContent,
    DateTimeOffset CaptureDateTime,
    string SourceMode,
    string LanguageTag = "",
    string ImagePath = "",
    double PositionLeft = 0,
    double PositionTop = 0,
    double PositionWidth = 0,
    double PositionHeight = 0);
