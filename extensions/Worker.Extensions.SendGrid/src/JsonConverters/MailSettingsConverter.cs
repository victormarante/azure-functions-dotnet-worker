using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SendGrid.Helpers.Mail;

namespace Microsoft.Azure.Functions.Worker
{
    public class MailSettingsConverter : JsonConverter<MailSettings>
    {
        public override MailSettings? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Read(ref reader, typeToConvert, options);

        public override void Write(Utf8JsonWriter writer, MailSettings value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("bcc") ?? "bcc");
            JsonSerializer.Serialize(writer, value.BccSettings, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("bypass_list_management") ?? "bypass_list_management");
            JsonSerializer.Serialize(writer, value.BypassListManagement, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("footer") ?? "footer");
            JsonSerializer.Serialize(writer, value.FooterSettings, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("sandbox_mode") ?? "sandbox_mode");
            JsonSerializer.Serialize(writer, value.SandboxMode, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("spam_check") ?? "spam_check");
            JsonSerializer.Serialize(writer, value.SpamCheck, options);

            writer.WriteEndObject();
        }
    }
}
