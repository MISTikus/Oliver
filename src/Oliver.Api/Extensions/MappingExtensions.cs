using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Oliver.Api.Extensions;

public static class MappingExtensions
{
    public static T Deserialize<T>(this byte[] bytes)
    {
        using MemoryStream ms = new(bytes);
        using BsonDataReader reader = new(ms);
        JsonSerializer serializer = new();
        return serializer.Deserialize<T>(reader);
    }

    public static byte[] Serialize<T>(this T value)
    {
        using MemoryStream ms = new();
        using BsonDataWriter writer = new(ms);
        JsonSerializer serializer = new();
        serializer.Serialize(writer, value);
        return ms.ToArray();
    }
}
