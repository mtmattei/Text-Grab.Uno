namespace TextGrab.Services;

/// <summary>
/// File I/O abstraction for cross-platform file operations.
/// </summary>
public interface IFileService
{
    Task<string?> PickAndReadTextFileAsync(CancellationToken ct = default);
    Task<bool> SaveTextFileAsync(string content, string? suggestedFileName = null, CancellationToken ct = default);
    Task<byte[]?> PickImageFileAsync(CancellationToken ct = default);
    Task<string> GetLocalStoragePathAsync(string fileName, CancellationToken ct = default);
}
