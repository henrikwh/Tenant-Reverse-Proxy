using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shared.Attributes;
using System.Net;
using System.Security.Claims;
using System.Text;
using Shared.Models;
using WeatherApi.Configuration;
using Microsoft.Identity.Web;
using Asp.Versioning;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement;
using Microsoft.Azure.Amqp.Framing;

var builder = WebApplication.CreateBuilder(args);
string env = String.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Development" : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
if (env == "Development")
    builder.Configuration.AddJsonFile($"{Environment.CurrentDirectory}/../Scripts/config.json", optional: false, reloadOnChange: true);

var logger = LoggerFactory.Create(config =>
{
    config.AddConsole();
    config.AddConfiguration(builder.Configuration.GetSection("Logging"));
}).CreateLogger("Program");

builder.AddExternalConfigurationStore();
builder.Services.AddFeatureManagement().AddFeatureFilter<PercentageFilter>().AddFeatureFilter<TimeWindowFilter>();
JwtSettings jwt = new JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwt);
builder.Services.AddHttpContextAccessor();

builder.Services.AddApplicationInsightsTelemetry();


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

})
.AddJwtBearer(options =>
           {
               options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuer = false,
                   ValidateAudience = false,
                   ValidateLifetime = false,
                   ValidateIssuerSigningKey = true,
                   ValidIssuer = jwt.ValidIssuer,
                   ValidAudience = jwt.ValidAudience,
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
               };

               options.Events = new JwtBearerEvents
               {
                   OnAuthenticationFailed = context =>
                   {
                       Console.WriteLine("OnAuthenticationFailed: " +
                           context.Exception.Message);
                       logger.LogError(context.Exception, context.Exception.Message);
                       return Task.CompletedTask;
                   },
                   OnTokenValidated = context =>
                   {
                       Console.WriteLine("OnTokenValidated: " +
                           context.SecurityToken);
                       logger.LogInformation("Login successful");
                       return Task.CompletedTask;
                   }
               };
           });

builder.Services.AddAuthorization(options =>
{

    options.AddPolicy("WebApi", policy =>
    {
        policy.AuthenticationSchemes.Add("Bearer");
        policy.RequireAuthenticatedUser();
    });
    options.AddPolicy("Test", policy => 
    {
        policy.Requirements.Add(new AuthRequirement());
    });

});

builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IAuthorizationHandler, AuthRequirementHandler>();
//// Add services to the container.


//#endregion

builder.Services.AddApiVersioning(
    o =>
    {
        //o.Conventions.Controller<UserController>().HasApiVersion(1, 0);
        o.ReportApiVersions = true;
        o.AssumeDefaultVersionWhenUnspecified = true;
        o.ApiVersionReader = new UrlSegmentApiVersionReader();
        o.DefaultApiVersion = new ApiVersion(1, 0);
    }
    );

// note: the specified format code will format the version as "'v'major[.minor][-status]"
//builder.Services.AddVersionedApiExplorer(
//options =>
//{
//    options.GroupNameFormat = "'v'VVVV";

//    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
//    // can also be used to control the format of the API version in route templates
//    options.SubstituteApiVersionInUrl = true;

//});


builder.Services.AddControllers();
builder.Services.AddProblemDetails();
var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();
app.UseExternalConfigurationStore();
app.UseAuthentication();

app.UseAuthorization();
//if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGet("/diag", (HttpRequest req, ClaimsPrincipal user) =>
{

    Dictionary<string, string> headers = new();
    Dictionary<string, string> cookies = new();
    Dictionary<string, string> claims = new();
    Dictionary<string, KeyValuePair<string, object?>> routes = new();


    foreach (var header in req.Headers)
    {
        headers.Add(header.Key, header.Value!);
    }
    foreach (var cookie in req.Cookies)
    {
        cookies.Add(cookie.Key, cookie.Value);
    }
    foreach (var route in req.RouteValues)
    {
        routes.Add(route.Key, route);
    }
    if (user.Identity!.IsAuthenticated)
        foreach (var claim in user.Claims)
        {
            claims.Add(claim.Type, claim.Value);
        }

    return new RequestDump(headers, cookies, claims, routes);


}).Produces(200, typeof(RequestDump), contentType: "application/json");//.RequireAuthorization();


app.Run();
