using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.Client;
using SdkMcpClient = ModelContextProtocol.Client.McpClient;
using System.Text.Json;
using TsxApi.Configuration;
using TsxApi.McpClient;
using TsxApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

builder.Services.Configure<McpServerOptions>(
    builder.Configuration.GetSection(McpServerOptions.SectionName));

builder.Services.Configure<ApiKeyOptions>(
    builder.Configuration.GetSection(ApiKeyOptions.SectionName));

builder.Services.AddHealthChecks();

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IMcpClient>(sp =>
{
    var opts = sp.GetRequiredService<IOptions<McpServerOptions>>().Value;
    var transport = new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri(opts.McpServerUrl),
        TransportMode = HttpTransportMode.StreamableHttp,
        Name = "TSX MCP Server",
        ConnectionTimeout = TimeSpan.FromSeconds(opts.TimeoutSeconds),
    });
    var sdkClient = SdkMcpClient.CreateAsync(transport, null, sp.GetService<Microsoft.Extensions.Logging.ILoggerFactory>())
        .GetAwaiter().GetResult();
    return new McpClientService(new SdkMcpClientAdapter(sdkClient));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TSX Financial Intelligence API", Version = "v1" });
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Name = "x-api-key",
        Type = SecuritySchemeType.ApiKey,
        Description = "API key required for all endpoints except /health",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.RoutePrefix = "swagger");
}

app.UseHttpsRedirection();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new { status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy" }));
    }
});

app.Run();

public partial class Program { }
