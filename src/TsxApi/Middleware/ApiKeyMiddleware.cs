using Microsoft.Extensions.Options;
using System.Text.Json;
using TsxApi.Configuration;
using TsxApi.Models;

namespace TsxApi.Middleware;

public sealed class ApiKeyMiddleware
{
    private static readonly string[] ExcludedPrefixes = ["/health", "/swagger"];

    private readonly RequestDelegate _next;
    private readonly IOptions<ApiKeyOptions> _options;

    public ApiKeyMiddleware(RequestDelegate next, IOptions<ApiKeyOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (ExcludedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("x-api-key", out var providedKey))
        {
            await WriteUnauthorized(context, "Missing x-api-key header.");
            return;
        }

        if (!string.Equals(providedKey, _options.Value.ApiKey, StringComparison.Ordinal))
        {
            await WriteUnauthorized(context, "Invalid API key.");
            return;
        }

        await _next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new ErrorResponse(401, message),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
