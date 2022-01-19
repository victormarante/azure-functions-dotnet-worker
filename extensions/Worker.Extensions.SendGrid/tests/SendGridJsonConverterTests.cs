using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using SendGrid;
using SendGrid.Helpers.Mail;
using Xunit;

namespace Worker.Extensions.SendGrid.Tests
{
    public class SendGridJsonConverterTests
    {
        [Fact]
        public void STJ_Produces_Json_Using_Custom_Converters()
        {
            JsonSerializerOptions? options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            options.Converters.Add(new PersonalizationConverter());
            options.Converters.Add(new SendGridMessageConverter());
            options.Converters.Add(new AttachmentConverter());
            options.Converters.Add(new AsmConverter());
            options.Converters.Add(new MailSettingsConverter());

            var message = new SendGridMessage();
            message.SetFrom(new EmailAddress("foo@bar.com", "Foo"));
            message.SetGlobalSubject("Global subject");
            message.AddTo(new EmailAddress("bar@foo.com", "Bar1"), 0, new Personalization
            {
                Subject = "Subject1",
                Substitutions = new Dictionary<string, string>()
                {
                    { "-nameplaceholder-", "Bar" }
                }
            });
            message.AddCc(new EmailAddress("bar@foo.net", "Bar2"));
            message.AddContent(MimeType.Text, "Hi -nameplaceholder-, Message content");
            message.BatchId = "mybatch";
            message.SendAt = DateTime.UtcNow.Ticks;

            // Use Newtonsoft JSON serializer to produce JSON string which we will use as expected value for assertion.
            var newtonSoftJsonString = Newtonsoft.Json.JsonConvert.SerializeObject(message);
            var stjJsonString = System.Text.Json.JsonSerializer.Serialize(message, options);

            Assert.Equal(newtonSoftJsonString, stjJsonString);
        }
    }
}
