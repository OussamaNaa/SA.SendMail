using MediatR;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using SA.SendEmails.ServiceEngines.Management.SendMail.Commands;
using SA.SendEmails.ServiceEngines.Management.SendMail.Responses;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text.Json.Serialization;
using static System.CoreConstants;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

webApplicationBuilder.Services.AddRequestLocalization(rl =>
{
    CultureInfo[] cultureInfos = new[]
    {
        new CultureInfo(LanguageCodes.Arabic),
        new CultureInfo(LanguageCodes.French),
        new CultureInfo(LanguageCodes.English)
    };

    rl.DefaultRequestCulture = new RequestCulture(cultureInfos[0], cultureInfos[0]);
    rl.SupportedCultures = cultureInfos;
    rl.SupportedUICultures = cultureInfos;

    rl.AddInitialRequestCultureProvider(new CustomRequestCultureProvider(async context =>
    {
        string languageCode = context.Request.Headers["LanguageCode"].ToString();

        if (languageCode.IsNullOrWhiteSpace()
        || (languageCode != LanguageCodes.Arabic
        && languageCode != LanguageCodes.French
        && languageCode != LanguageCodes.English))
        {
            return new ProviderCultureResult(webApplicationBuilder.Configuration["DefaultLanguageCode"]);
        }

        return new ProviderCultureResult(languageCode);
    }));
});

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
        sg.SwaggerDoc("v1", new OpenApiInfo() { Title = "SA.ReinsurancePlatform.Services.LateralService", Version = "v1" });
       
        sg.AddSecurityRequirement(new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme()
                {
                    Reference = new OpenApiReference()
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme
                    }
                },
                new string[] {}
            }
        });
        sg.IgnoreObsoleteActions();
        sg.IgnoreObsoleteProperties();
    });
}



webApplicationBuilder.Services.AddTransient<System.ILogger, Log4NetLogger>();

webApplicationBuilder.Services.AddTransient<IElectronicMailSender, ElectronicMailSender>();


webApplicationBuilder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

webApplicationBuilder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(typeof(Program).GetTypeInfo().Assembly));

webApplicationBuilder.Services.AddScoped<IRequestHandler<SendMailCommands, SendMailResponse>, SendMailCommandsHandler>();

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


webApplication.UseCors(c =>
{
    c.WithOrigins(webApplicationBuilder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>());
    c.AllowAnyHeader();
    c.AllowAnyMethod();
    c.AllowCredentials();
});

webApplication.UseResponseCaching();


webApplication.UseRouting();

webApplication.MapControllers();

webApplication.Run();