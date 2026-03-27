using System.Text.Json;

namespace TextGrab.Shared;

public static class Json
{
    public static async Task<T?> ToObjectAsync<T>(string value)
    {
        return await Task.Run(() => JsonSerializer.Deserialize<T>(value));
    }

    public static async Task<string> StringifyAsync(object value)
    {
        return await Task.Run(() => JsonSerializer.Serialize(value));
    }
}
