using System.Reflection;
using System.Text.Json.Serialization;
using BCD.Payphone.Api.Policies;
using BCD.Payphone.Data;
using BCD.Payphone.Logic;
using BCD.Payphone.Svc;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

// Setup Custom Configuration
var configuration = builder.BindDuallyNoteConfiguration();

// Add services to the container and define auth policy filters
builder.Services.AddControllers().AddJsonOptions(opts =>
{
    var enumConverter = new JsonStringEnumConverter();
    opts.JsonSerializerOptions.Converters.Add(enumConverter);
});

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Add Custom Services
if (!string.IsNullOrWhiteSpace(configuration.Database?.Server))
{
    builder.Services.AddSingleton<IDatabaseClientFactory, MongoClientFactory>();
}

builder.Services.AddAuthentication("Twilio")
        .AddScheme<TokenAuthenticationOptions, TwilioAuthenticationHandler>("Twilio", null);

builder.Services.AddSingleton<ICustomAuthenticationManager, CustomAuthenticationManager>();
builder.Services.AddTransient<ICallLogic, CallLogic>();
builder.Services.AddTransient<ISpotifyLogic, SpotifyLogic>();
builder.Services.AddTransient<IHubitatLogic, HubitatLogic>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddHangfire(x => x.UseMemoryStorage());
builder.Services.AddHangfireServer();

builder.Host.UseSerilog((ctx, lc) => lc
    .WriteTo.Console(new RenderedCompactJsonFormatter())
    .ReadFrom.Configuration(ctx.Configuration));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Payphone API",
        Description = "API Actions supporting Payphone Party Project"
    });

    // using System.Reflection;
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard();
}

app.UseAuthentication();
app.UseAuthorization();
// Add this line; you'll need `using Serilog;` up the top, too
app.UseSerilogRequestLogging(opts =>
{
    opts.IncludeQueryInRequestPath = true;
    opts.EnrichDiagnosticContext = async (diagnosticsContext, httpContext) =>
    {
        var request = httpContext.Request;
        var parameters = new Dictionary<string, string>();
        if (request.HasFormContentType)
        {
            var form = await request.ReadFormAsync();
            parameters = form.ToDictionary(p => p.Key, p => p.Value.ToString());
        }

        if(request.Path.HasValue && request.Path.Value.Contains("process-command"))
        {
            if(parameters.TryGetValue("Digits", out var digits))
            {
                var command = "Unknown";
                switch(digits)
                {
                    case "1":
                        command = "Song Request";
                        break;
                    case "2":
                        command = "Light Change";
                        break;
                    case "3":
                        command = "Joke";
                        break;
                    case "4":
                        command = "Record Message";
                        break;
                    case "5":
                        command = "Surprise Call";
                        break;
                    case "6":
                        command = "Repeat Message";
                        break;
                    case "*":
                        command = "Admin Call";
                        break;
                }
                diagnosticsContext.Set("ProcessedCommand", command);
            }
        }

        diagnosticsContext.Set("Headers", request.Headers);
        diagnosticsContext.Set("FormParameters", parameters);
        diagnosticsContext.Set("RemoteIp", request.HttpContext.Connection.RemoteIpAddress?.ToString());
    };

});

app.MapControllers();

app.Run();


