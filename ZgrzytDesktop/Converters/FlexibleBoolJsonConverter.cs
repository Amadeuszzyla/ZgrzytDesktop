using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZgrzytDesktop.Converters;

public class FlexibleBoolJsonConverter : JsonConverter<bool>
{
    public override bool Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True)
            return true;

        if (reader.TokenType == JsonTokenType.False)
            return false;

        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt32() != 0;

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();

            if (bool.TryParse(value, out var boolValue))
                return boolValue;

            if (int.TryParse(value, out var intValue))
                return intValue != 0;
        }

        return false;
    }

    public override void Write(
        Utf8JsonWriter writer,
        bool value,
        JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}    
    