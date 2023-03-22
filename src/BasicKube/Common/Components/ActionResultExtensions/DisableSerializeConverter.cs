using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BasicKube.Api.Common.Components.ActionResultExtensions
{
    public class DisableSerializeConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null; // Or throw an exception if you don't want to allow null
            else if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
            var arr = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                    return arr;
                else if (reader.TokenType == JsonTokenType.String)
                    arr.Add(reader.GetString());
                else
                {
                    throw new JsonException(); // Unexpected token;
                }
            }
            //return new List<string>();

            throw new JsonException(); // Truncated file;
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
#if DEBUG
            writer.WriteStartArray();
            foreach (var item in value)
                writer.WriteStringValue(item);
            writer.WriteEndArray();
#endif

        }
    }
}
