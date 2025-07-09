using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SA.SendEmails.ServiceEngines.Management.SendMail.Commands;
using System.Reflection;
using System.Text.Json.Serialization;
using static System.CoreConstants;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.WebHost.ConfigureKestrel(k =>
{
    k.AddServerHeader = false;
    k.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    k.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(5);
    k.Limits.MaxConcurrentConnections = 100;
    k.Limits.MaxConcurrentUpgradedConnections = 100;
});

webApplicationBuilder.Services.AddControllers(c =>
{
    c.CacheProfiles.Add(Generals.DefaultCacheProfile, new CacheProfile()
    {
        Duration = 14400,
        Location = ResponseCacheLocation.Any,
        NoStore = false
    });
}).AddJsonOptions(jo =>
{
    jo.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    jo.JsonSerializerOptions.IgnoreReadOnlyFields = true;
    jo.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
    jo.JsonSerializerOptions.IncludeFields = false;
    jo.JsonSerializerOptions.NumberHandling = JsonNumberHandling.Strict;
    jo.JsonSerializerOptions.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
    jo.JsonSerializerOptions.WriteIndented = false;
});


webApplicationBuilder.Services.AddResponseCaching(rc =>
{
    rc.SizeLimit = 1073741824;
    rc.MaximumBodySize = 1073741824;
    rc.UseCaseSensitivePaths = true;
});

webApplicationBuilder.Services.AddEndpointsApiExplorer();

if (bool.Parse(webApplicationBuilder.Configuration["Swagger:IsEnabled"]))
{
    webApplicationBuilder.Services.AddSwaggerGen(sg =>
    {
        sg.SwaggerDoc("v1", new OpenApiInfo() { Title = "SA.SendMail.Services.LateralService", Version = "v1" });
               
        sg.IgnoreObsoleteActions();
        sg.IgnoreObsoleteProperties();
    });
}

webApplicationBuilder.Services.AddTransient<System.ILogger, Log4NetLogger>();

webApplicationBuilder.Services.AddTransient<IElectronicMailSender, ElectronicMailSender>();

LoggerHelper.Initialization(new Log4NetLogger());

webApplicationBuilder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

webApplicationBuilder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(SendMailCommands).GetTypeInfo().Assembly));

webApplicationBuilder.Services.AddHttpContextAccessor();

webApplicationBuilder.Services.AddTransient<SendAccessTokenHandler>();


webApplicationBuilder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

#region Repositories injection

#endregion Repositories injection

WebApplication webApplication = webApplicationBuilder.Build();

webApplication.UseRequestLocalization();

if (bool.Parse(webApplicationBuilder.Configuration["Swagger:IsEnabled"]))
{
    webApplication.UseSwagger();
    webApplication.UseSwaggerUI();
}

webApplication.UseResponseCaching();

webApplication.MapControllers();

webApplication.Run();