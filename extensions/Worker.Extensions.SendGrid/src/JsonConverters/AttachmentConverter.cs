using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SendGrid.Helpers.Mail;

namespace Microsoft.Azure.Functions.Worker
{
    public class AttachmentConverter : JsonConverter<Attachment>
    {
        public override Attachment? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, Attachment value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(value.Content)) ?? nameof(value.Content), value.Content);
            writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(value.Type)) ?? nameof(value.Type), value.Type);
            writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(value.Filename)) ?? nameof(value.Filename), value.Filename);
            writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(value.Disposition)) ?? nameof(value.Disposition), value.Disposition);
            writer.WriteString(options.PropertyNamingPolicy?.ConvertName("content_id") ?? "content_id", value.ContentId);

            writer.WriteEndObject();
        }
    }
}
