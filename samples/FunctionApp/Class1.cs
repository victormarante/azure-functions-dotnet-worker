// Auto-generated code
using System;
using System.Collections.Generic;
namespace FunctionApp
{
    internal class MyInternalType
    {

    }
       

    [StartupInfo]
    internal class ExtensionStartupDataProviderManualInternal
    {
        public Dictionary<string, string> GetItems()
        {
            var dict = new Dictionary<string, string>(1);
            dict.Add("Microsoft.Azure.Functions.Worker.Extensions.Http.MyHttpExtensionStartup", "Microsoft.Azure.Functions.Worker.Extensions.Http, Version=4.0.5.0, Culture=neutral, PublicKeyToken=551316b6919f366c");
            return dict;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class StartupInfoAttribute : Attribute
    {
    }

}
