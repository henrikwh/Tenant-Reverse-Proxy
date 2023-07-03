using System.Reflection;
using Yarp.ReverseProxy.Configuration;

namespace SaaS.Proxy.Configuration  
{
    public static class OpenApi
    {
        public static void AddOpenApi(this WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                Assembly currentAssembly = Assembly.GetExecutingAssembly();
                var xmlDocs = currentAssembly.GetReferencedAssemblies()
                            .Union(new AssemblyName[] { currentAssembly.GetName() })
                            .Select(a => Path.Combine(Path.GetDirectoryName(currentAssembly.Location) ?? throw new NullReferenceException(), $"{a.Name}.xml"))
                            .Where(f => File.Exists(f)).ToArray();
                Array.ForEach(xmlDocs, (d) =>
                {
                    options.IncludeXmlComments(d);
                });

            });
        }
        public static void UseOpenApi(this WebApplication app)
        {
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
        }

    }
}
