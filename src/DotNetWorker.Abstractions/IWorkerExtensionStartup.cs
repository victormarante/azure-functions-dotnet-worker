using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{
    public interface IWorkerExtensionStartup

    {
        /// <summary>
        /// C
        /// </summary>IFunctionsWorkerApplicationBuilder
        /// <param name="applicationBuilder"></param>
        void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder);
    }
}
