namespace AccreditValidation.Converters
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Converts JSON values to nullable long, handling cases where the value might be a string or empty.
    /// </summary>
    public class NullableLongConverter : JsonConverter<long?>
    {
        public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
    // Handle null values
if (reader.TokenType == JsonTokenType.Null)
     {
                return null;
   }

     // Handle numeric values directly
       if (reader.TokenType == JsonTokenType.Number)
        {
   return reader.GetInt64();
         }

 // Handle string values
    if (reader.TokenType == JsonTokenType.String)
  {
   var stringValue = reader.GetString();
      
   // Return null for empty or whitespace strings
  if (string.IsNullOrWhiteSpace(stringValue))
        {
     return null;
            }

       // Try to parse the string as a long
       if (long.TryParse(stringValue, out long result))
     {
        return result;
                }

    // If parsing fails, return null instead of throwing
       return null;
            }

          // For any other unexpected type, return null
            return null;
        }

        public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
 {
          if (value.HasValue)
       {
                writer.WriteNumberValue(value.Value);
        }
    else
    {
     writer.WriteNullValue();
   }
     }
    }
}
