using System.Text.Json;
using System.Text.Json.Serialization;

namespace BasicKube.Models.JsonConverters;

public class PromMetricsValueConverter
    : JsonConverter<List<Tuple<double, string>>>
{
    public override List<Tuple<double, string>>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert, 
        JsonSerializerOptions options)
    {
        List<Tuple<double, string>> result = new();

        var str = reader.GetString() ?? "[]";
        str = str.Substring(1, str.Length - 2);
        var items = str.Split(",");
        for (int i = 0; i < items.Length; i+=2)
        {
            var time = items[i];
            time = time.Substring(1);
            var val = items[i + 1];
            val = val.Substring(0, val.Length - 1);
            result.Add(Tuple.Create(double.Parse(time), val));
        }

        return result;
    }

    public override void Write(
        Utf8JsonWriter writer,
        List<Tuple<double, string>> value, 
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (Tuple<double, string> tuple in value)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(tuple.Item1);
            writer.WriteStringValue(tuple.Item2);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }
}
