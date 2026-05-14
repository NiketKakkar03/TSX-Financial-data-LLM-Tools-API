using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;

namespace TsxApi.Tests;

public sealed class ApiKeyMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string TestApiKey = "test-secret-key";

    private readonly WebApplicationFactory<Program> _factory;

    public ApiKeyMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ApiKey:ApiKey", TestApiKey);
        });
    }

    [Fact]
    public async Task MissingApiKey_Returns401WithErrorShape()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/stocks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal(401, doc.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(doc.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task WrongApiKey_Returns401WithErrorShape()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", "wrong-key");

        var response = await client.GetAsync("/api/stocks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal(401, doc.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(doc.GetProperty("message").GetString()));
    }

    [Fact]
    public async Task CorrectApiKey_PassesThroughMiddleware()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", TestApiKey);

        var response = await client.GetAsync("/api/stocks");

        // Middleware passed through — routing may return 404 since the endpoint is not yet implemented,
        // but it must not be 401.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthEndpoint_ReachableWithoutApiKey()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerEndpoint_ReachableWithoutApiKey()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/swagger");

        // Swagger redirects to /swagger/index.html — either 200 or 3xx is acceptable, never 401.
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
