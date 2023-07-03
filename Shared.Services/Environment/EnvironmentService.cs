using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Environment
{
    public interface IEnvironmentService
    {
        string GetEnvironmentName();
    }

    public class EnvironmentService : IEnvironmentService
    {
        public string GetEnvironmentName()
        {
            string env = String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Development" : System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            return env;
        }
    }
}
