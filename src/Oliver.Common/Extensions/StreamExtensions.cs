namespace Oliver.Common.Extensions;

public static class StreamExtensions
{
    public static async Task<string> ReadAsStringAsync(this Stream stream, string nullValue = null, bool toBase64 = false)
    {
        if (stream is null)
            return nullValue;

        if (toBase64)
        {
            var bytes = await stream.ReadAllBytesAsync();
            var base64 = Convert.ToBase64String(bytes);
            return base64;
        }

        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync();
    }

    private static async Task<byte[]> ReadAllBytesAsync(this Stream input)
    {
        var buffer = new byte[16 * 1024];
        using MemoryStream ms = new();
        int read;
        while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length))) > 0)
        {
            await ms.WriteAsync(buffer.AsMemory(0, read));
        }
        return ms.ToArray();
    }
}
