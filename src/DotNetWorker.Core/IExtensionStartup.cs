namespace Microsoft.Azure.Functions.Worker.Core
{
    public interface IWorkerExtensionStartup

    {
        /// <summary>
        /// C
        /// </summary>
        /// <param name="applicationBuilder"></param>
        void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder);
    }

    public class MyCoreCopyStartup : IWorkerExtensionStartup
    {
        public void Configure(IFunctionsWorkerApplicationBuilder applicationBuilder)
        {

        }
    }
}
