using System.Text.Json;

namespace TextGrab.Services;

public class FileHistoryService : IHistoryService
{
    private const string HistoryFileName = "text-grab-history.json";
    private List<HistoryInfo> _textHistory = [];
    private List<HistoryInfo> _imageHistory = [];
    private bool _loaded;

    private static string GetHistoryFilePath()
    {
        var folder = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        return Path.Combine(folder, HistoryFileName);
    }

    public async Task LoadHistoriesAsync(CancellationToken ct = default)
    {
        if (_loaded) return;

        var path = GetHistoryFilePath();
        if (!File.Exists(path))
        {
            _loaded = true;
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, ct);
            var items = JsonSerializer.Deserialize<List<HistoryInfo>>(json);
            if (items is not null)
            {
                _textHistory = items.Where(h => string.IsNullOrEmpty(h.ImagePath)).ToList();
                _imageHistory = items.Where(h => !string.IsNullOrEmpty(h.ImagePath)).ToList();
            }
        }
        catch
        {
            // If history file is corrupt, start fresh
            _textHistory = [];
            _imageHistory = [];
        }

        _loaded = true;
    }

    public async Task WriteHistoryAsync(CancellationToken ct = default)
    {
        var all = _textHistory.Concat(_imageHistory).OrderByDescending(h => h.CaptureDateTime).ToList();
        var json = JsonSerializer.Serialize(all, new JsonSerializerOptions { WriteIndented = true });
        var path = GetHistoryFilePath();
        await File.WriteAllTextAsync(path, json, ct);
    }

    public IReadOnlyList<HistoryInfo> GetTextHistory() => _textHistory.AsReadOnly();

    public IReadOnlyList<HistoryInfo> GetImageHistory() => _imageHistory.AsReadOnly();

    public async Task SaveTextHistoryAsync(HistoryInfo item, CancellationToken ct = default)
    {
        await LoadHistoriesAsync(ct);

        if (string.IsNullOrEmpty(item.ImagePath))
            _textHistory.Insert(0, item);
        else
            _imageHistory.Insert(0, item);

        // Keep max 100 items per list
        if (_textHistory.Count > 100)
            _textHistory.RemoveRange(100, _textHistory.Count - 100);
        if (_imageHistory.Count > 100)
            _imageHistory.RemoveRange(100, _imageHistory.Count - 100);

        await WriteHistoryAsync(ct);
    }

    public async Task RemoveHistoryItemAsync(HistoryInfo item, CancellationToken ct = default)
    {
        _textHistory.Remove(item);
        _imageHistory.Remove(item);
        await WriteHistoryAsync(ct);
    }

    public Task DeleteAllHistoryAsync(CancellationToken ct = default)
    {
        _textHistory.Clear();
        _imageHistory.Clear();

        var path = GetHistoryFilePath();
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }
}
