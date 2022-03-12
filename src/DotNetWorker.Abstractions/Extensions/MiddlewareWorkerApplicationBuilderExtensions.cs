using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Worker
{
    public static class MiddlewareWorkerApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the <see cref="IFunctionsWorkerApplicationBuilder"/> to use the provided middleware type.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <returns>The same instance of the <see cref="IFunctionsWorkerApplicationBuilder"/> for chanining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseMiddleware<T>(this IFunctionsWorkerApplicationBuilder builder)
            where T : class, IFunctionsWorkerMiddleware
        {
            builder.Services.AddSingleton<T>();

            builder.Use(next =>
            {
                return context =>
                {
                    var middleware = context.InstanceServices.GetRequiredService<T>();

                    return middleware.Invoke(context, next);
                };
            });

            return builder;
        }

        /// <summary>
        /// Configures the <see cref="IFunctionsWorkerApplicationBuilder"/> to use the provided inline middleware delegate.
        /// </summary>
        /// <param name="builder">The <see cref="IFunctionsWorkerApplicationBuilder"/> to configure.</param>
        /// <param name="middleware">The middleware to add to the invocation pipeline.</param>
        /// <returns>The same <see cref="IFunctionsWorkerApplicationBuilder"/> for chaining.</returns>
        public static IFunctionsWorkerApplicationBuilder UseMiddleware(this IFunctionsWorkerApplicationBuilder builder, Func<FunctionContext, Func<Task>, Task> middleware)
        {

            builder.Use(next =>
            {
                return context =>
                {
                    return middleware(context, () => next.Invoke(context));
                };
            });

            return builder;
        }
    }
}
