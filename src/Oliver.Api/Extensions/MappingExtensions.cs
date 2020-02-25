using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using System.IO;

namespace Oliver.Api.Extensions
{
    public static class MappingExtensions
    {
        public static T Deserialize<T>(this byte[] bytes)
        {
            using var ms = new MemoryStream(bytes);
            using var reader = new BsonReader(ms);
            var serializer = new JsonSerializer();
            return serializer.Deserialize<T>(reader);
        }
        public static byte[] Serialize<T>(this T value)
        {
            using var ms = new MemoryStream();
            using var writer = new BsonWriter(ms);
            var serializer = new JsonSerializer();
            serializer.Serialize(writer, value);
            return ms.ToArray();
        }
    }
}
