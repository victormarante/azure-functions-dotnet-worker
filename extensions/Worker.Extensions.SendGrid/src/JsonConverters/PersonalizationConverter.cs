using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SendGrid.Helpers.Mail;

namespace Microsoft.Azure.Functions.Worker
{
    public class PersonalizationConverter : JsonConverter<Personalization>
    {
        public override Personalization Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, Personalization value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("to") ?? "to");
            JsonSerializer.Serialize(writer, value.Tos, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("cc") ?? "cc");
            JsonSerializer.Serialize(writer, value.Ccs, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("bcc") ?? "bcc");
            JsonSerializer.Serialize(writer, value.Bccs, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.From)) ?? nameof(value.From));
            JsonSerializer.Serialize(writer, value.From, options);

            writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(value.Subject)) ?? nameof(value.Subject), value.Subject);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Headers)) ?? nameof(value.Headers));
            JsonSerializer.Serialize(writer, value.Headers, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Substitutions)) ?? nameof(value.Substitutions));
            JsonSerializer.Serialize(writer, value.Substitutions, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("custom_args") ?? "custom_args");
            JsonSerializer.Serialize(writer, value.CustomArgs, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("send_at") ?? "send_at");
            JsonSerializer.Serialize(writer, value.SendAt, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("dynamic_template_data") ?? "dynamic_template_data");
            JsonSerializer.Serialize(writer, value.TemplateData, options);

            writer.WriteEndObject();
        }
    }
}
