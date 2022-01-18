using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SendGrid.Helpers.Mail;

namespace Microsoft.Azure.Functions.Worker
{
    public class AsmConverter : JsonConverter<ASM>
    {
        public override ASM? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, ASM value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteNumber(options.PropertyNamingPolicy?.ConvertName("group_id") ?? "group_id", value.GroupId);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("groups_to_display") ?? "groups_to_display");
            JsonSerializer.Serialize(writer, value.GroupsToDisplay, options);

            writer.WriteEndObject();
        }
    }
}
