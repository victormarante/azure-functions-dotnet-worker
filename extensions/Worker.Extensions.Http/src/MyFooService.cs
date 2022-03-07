using System;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Azure.Functions.Worker.Extensions.Http
{
    public class MyFooService : IMyFooService
    {
        private string _msg;
        private IConfiguration _configuration;
        public MyFooService(IConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _msg = DateTime.Now.ToString();
        }
        public string GetFooMessage()
        {
           if(_configuration["Foo"]!=null)
            {
                return _configuration["Foo"].ToString();
            }

            return $"Hello {_msg}";
        }
    }

    public interface IMyFooService
    {
        string GetFooMessage();
    }
}
