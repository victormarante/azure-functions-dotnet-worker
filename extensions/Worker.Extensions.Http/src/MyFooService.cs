using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http
{
    public class MyFooService : IMyFooService
    {
        private string _msg;
        public MyFooService()
        {
            _msg = DateTime.Now.ToString();
        }
        public string GetFooMessage()
        {

            return $"Hello {_msg}";
        }
    }

    public interface IMyFooService
    {
        string GetFooMessage();
    }
}
