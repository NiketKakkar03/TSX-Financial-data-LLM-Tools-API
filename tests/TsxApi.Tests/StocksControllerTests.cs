using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Net;
using System.Text.Json;
using TsxApi.McpClient;
using TsxApi.McpClient.Models;

namespace TsxApi.Tests;

public sealed class StocksControllerTests
{
    private const string TestApiKey = "test-key";

    private static WebApplicationFactory<Program> BuildFactory(IMcpClient mcpClient) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("ApiKey:ApiKey", TestApiKey);
            b.ConfigureTestServices(services =>
                services.AddSingleton(mcpClient));
        });

    private static HttpClient AuthClient(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", TestApiKey);
        return client;
    }

    [Fact]
    public async Task GetCompanies_Returns200WithJsonArray()
    {
        var companies = new[]
        {
            new CompanyListItem("RY.TO", "Royal Bank", "Financials"),
            new CompanyListItem("TD.TO", "TD Bank", "Financials"),
        };
        var mock = Substitute.For<IMcpClient>();
        mock.GetCompanyListAsync(default).ReturnsForAnyArgs(companies);

        using var factory = BuildFactory(mock);
        var response = await AuthClient(factory).GetAsync("/api/stocks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        var items = JsonDocument.Parse(body).RootElement.EnumerateArray().ToList();
        Assert.Equal(2, items.Count);
        Assert.Equal("RY.TO", items[0].GetProperty("ticker").GetString());
        Assert.Equal("Royal Bank", items[0].GetProperty("name").GetString());
        Assert.Equal("Financials", items[0].GetProperty("sector").GetString());
    }

    [Fact]
    public async Task GetCompanies_MissingApiKey_Returns401()
    {
        var mock = Substitute.For<IMcpClient>();
        using var factory = BuildFactory(mock);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/stocks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCompanies_McpServerUnreachable_Returns502()
    {
        var mock = Substitute.For<IMcpClient>();
        mock.GetCompanyListAsync(default)
            .ThrowsAsyncForAnyArgs(new HttpRequestException("Connection refused"));

        using var factory = BuildFactory(mock);
        var response = await AuthClient(factory).GetAsync("/api/stocks");

        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body).RootElement;
        Assert.Equal(502, doc.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(doc.GetProperty("message").GetString()));
    }
}
