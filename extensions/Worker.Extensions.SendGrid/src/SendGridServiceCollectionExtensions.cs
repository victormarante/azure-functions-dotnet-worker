using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker.Extensions.SendGrid
{
    public static class SendGridServiceCollectionExtensions
    {
        public static IServiceCollection AddSendGrid(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException("services");
            }

            // Register the STJ converters which produces the JSON similar to what JSON.NET produces.
            // See https://github.com/Azure/azure-functions-dotnet-worker/issues/737#issuecomment-984267501 for details
            services.Configure<JsonSerializerOptions>(jsonSerializerOptions =>
            {
                jsonSerializerOptions.Converters.Add(new SendGridMessageConverter());
                jsonSerializerOptions.Converters.Add(new PersonalizationConverter());
                jsonSerializerOptions.Converters.Add(new AttachmentConverter());
                jsonSerializerOptions.Converters.Add(new AsmConverter());
                jsonSerializerOptions.Converters.Add(new MailSettingsConverter());
            });

            return services;
        }
    }
}
