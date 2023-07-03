

using Asp.Versioning;
using ControlPlane.Configuration;
using Microsoft.Extensions.Options;
using Shared.Services.Environment;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;
using System.Text.Json;
using TenantManagement.Repositories;


namespace TenantManagement
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            //for local development, load the config file created duing enlistment
            string env = String.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Development" : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            if (env == "Development")
                builder.Configuration.AddJsonFile($"{Environment.CurrentDirectory}/../../Scripts/config.json", optional: false, reloadOnChange: true);

            builder.AddExternalConfigurationStore();
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;

            });
            builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);

            builder.Services.AddSingleton<ITenantRepositoryReadWrite, TenantRepositoryReadWrite>();
            builder.Services.AddSingleton<IConfigurationRoot>(builder.Configuration);
            builder.Services.AddSingleton<IEnvironmentService, EnvironmentService>();
            builder.Services.AddProblemDetails();

            builder.Services.AddApiVersioning(
                                options =>
                                {
                                    // reporting api versions will return the headers
                                    // "api-supported-versions" and "api-deprecated-versions"
                                    options.DefaultApiVersion = new ApiVersion(1, 0);
                                    options.AssumeDefaultVersionWhenUnspecified = true;
                                    options.ReportApiVersions = true;
                                    options.ApiVersionReader = new UrlSegmentApiVersionReader();
                                    options.Policies.Sunset(0.9)
                                                    .Effective(DateTimeOffset.Now.AddDays(60))
                                                    .Link("policy.html")
                                                        .Title("Versioning Policy")
                                                        .Type("text/html");
                                })
                            .AddMvc()
                            .AddApiExplorer(
                                options =>
                                {
                                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                                    options.GroupNameFormat = "'v'VVV";
                                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                                    // can also be used to control the format of the API version in route templates
                                    options.SubstituteApiVersionInUrl = true;
                                    options.AssumeDefaultVersionWhenUnspecified = true;
                                    options.SubstitutionFormat = "VVVV";
                                    options.DefaultApiVersion = new ApiVersion(1, 0);

                                });

            builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            builder.Services.AddSwaggerGen(
                options =>
                {

                    // add a custom operation filter which sets default values
                    //options.OperationFilter<SwaggerDefaultValues>();
                    var fileName = typeof(Program).Assembly.GetName().Name + ".xml";
                    var filePath = Path.Combine(AppContext.BaseDirectory, fileName);

                    // integrate xml comments
                    options.IncludeXmlComments(filePath);
                });
            var app = builder.Build();
            app.UseExternalConfigurationStore();
            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(
                options =>
                {
                    var descriptions = app.DescribeApiVersions();
                    // build a swagger endpoint for each discovered API version
                    foreach (var description in descriptions.OrderByDescending(o => o.ApiVersion))
                    {
                        var url = $"/swagger/{description.GroupName}/swagger.json";
                        var name = description.GroupName.ToUpperInvariant();
                        options.SwaggerEndpoint(url, name);

                    }
                });
            }

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
            Console.ReadKey();
        }
    }
}