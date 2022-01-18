using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using SendGrid.Helpers.Mail;

namespace Microsoft.Azure.Functions.Worker
{
    public class SendGridMessageConverter : JsonConverter<SendGridMessage>
    {
        public override SendGridMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return Read(ref reader, typeToConvert, options);
        }

        public override void Write(Utf8JsonWriter writer, SendGridMessage value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.From)) ?? nameof(value.From));
            JsonSerializer.Serialize(writer, value.From, options);

            writer.WriteString(options.PropertyNamingPolicy?.ConvertName(nameof(value.Subject)) ?? nameof(value.Subject), value.Subject);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Personalizations)) ?? nameof(value.Personalizations));
            JsonSerializer.Serialize(writer, value.Personalizations, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("content") ?? "content");
            JsonSerializer.Serialize(writer, value.Contents, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Attachments)) ?? nameof(value.Attachments));
            JsonSerializer.Serialize(writer, value.Attachments, options);

            writer.WriteString(options.PropertyNamingPolicy?.ConvertName("template_id") ?? "template_id", value.TemplateId);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Headers)) ?? nameof(value.Headers));
            JsonSerializer.Serialize(writer, value.Headers, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Sections)) ?? nameof(value.Sections));
            JsonSerializer.Serialize(writer, value.Sections, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Categories)) ?? nameof(value.Categories));
            JsonSerializer.Serialize(writer, value.Categories, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("custom_args") ?? "custom_args");
            JsonSerializer.Serialize(writer, value.CustomArgs, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("send_at") ?? "send_at");
            JsonSerializer.Serialize(writer, value.SendAt, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName(nameof(value.Asm)) ?? nameof(value.Asm));
            JsonSerializer.Serialize(writer, value.Asm, options);

            writer.WriteString(options.PropertyNamingPolicy?.ConvertName("batch_id") ?? "batch_id", value.BatchId);

            writer.WriteString(options.PropertyNamingPolicy?.ConvertName("ip_pool_name") ?? "ip_pool_name", value.IpPoolName);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("mail_settings") ?? "mail_settings");
            JsonSerializer.Serialize(writer, value.MailSettings, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("tracking_settings") ?? "tracking_settings");
            JsonSerializer.Serialize(writer, value.TrackingSettings, options);

            writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("reply_to") ?? "reply_to");
            JsonSerializer.Serialize(writer, value.ReplyTo, options);

            writer.WriteEndObject();
        }
    }
}
