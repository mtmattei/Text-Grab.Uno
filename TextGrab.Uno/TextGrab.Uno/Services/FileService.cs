using Windows.Storage;
using Windows.Storage.Pickers;

namespace TextGrab.Services;

public class FileService : IFileService
{
    public async Task<string?> PickAndReadTextFileAsync(CancellationToken ct = default)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".txt");
        picker.FileTypeFilter.Add(".csv");
        picker.FileTypeFilter.Add(".md");
        picker.FileTypeFilter.Add(".log");
        picker.FileTypeFilter.Add(".json");
        picker.FileTypeFilter.Add(".xml");
        picker.FileTypeFilter.Add("*");

#if WINDOWS
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
#endif

        var file = await picker.PickSingleFileAsync();
        if (file is null) return null;

        return await FileIO.ReadTextAsync(file);
    }

    public async Task<bool> SaveTextFileAsync(string content, string? suggestedFileName = null, CancellationToken ct = default)
    {
        var picker = new FileSavePicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeChoices.Add("Text Files", [".txt"]);
        picker.FileTypeChoices.Add("All Files", ["."]);

        if (!string.IsNullOrEmpty(suggestedFileName))
            picker.SuggestedFileName = suggestedFileName;
        else
            picker.SuggestedFileName = "TextGrab Output";

#if WINDOWS
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
#endif

        var file = await picker.PickSaveFileAsync();
        if (file is null) return false;

        await FileIO.WriteTextAsync(file, content);
        return true;
    }

    public async Task<byte[]?> PickImageFileAsync(CancellationToken ct = default)
    {
        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".bmp");
        picker.FileTypeFilter.Add(".gif");
        picker.FileTypeFilter.Add(".tiff");

#if WINDOWS
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
#endif

        var file = await picker.PickSingleFileAsync();
        if (file is null) return null;

        var buffer = await FileIO.ReadBufferAsync(file);
        var bytes = new byte[buffer.Length];
        using var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer);
        reader.ReadBytes(bytes);
        return bytes;
    }

    public async Task<string> GetLocalStoragePathAsync(string fileName, CancellationToken ct = default)
    {
        var folder = ApplicationData.Current.LocalFolder;
        return Path.Combine(folder.Path, fileName);
    }
}
