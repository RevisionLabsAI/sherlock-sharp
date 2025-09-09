using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SherlockSharp.Models;

internal sealed class FlexibleStringArrayConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            return s == null ? Array.Empty<string>() : new[] { s };
        }
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<string>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;
                if (reader.TokenType == JsonTokenType.String)
                {
                    list.Add(reader.GetString() ?? string.Empty);
                }
                else
                {
                    // skip non-string entries
                    reader.Skip();
                }
            }
            return list.ToArray();
        }
        // Unknown token: skip
        reader.Skip();
        return Array.Empty<string>();
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var s in value)
            writer.WriteStringValue(s);
        writer.WriteEndArray();
    }
}
