using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Text.Json;
using TsxApi.McpClient;
using TsxApi.McpClient.Models;

namespace TsxApi.Tests;

public sealed class McpClientServiceTests
{
    private readonly ISdkMcpClientAdapter _sdk = Substitute.For<ISdkMcpClientAdapter>();
    private readonly McpClientService _sut;

    public McpClientServiceTests() => _sut = new McpClientService(_sdk);

    // ── helpers ──────────────────────────────────────────────────────────────

    private static ReadResourceResult ResourceResult(string json) => new()
    {
        Contents = [new TextResourceContents { Uri = "test://uri", Text = json }]
    };

    private static CallToolResult ToolResult(string text) => new()
    {
        Content = [new TextContentBlock { Text = text }]
    };

    // ── GetCompanyListAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetCompanyListAsync_CallsCorrectResource_AndMapsToTypedList()
    {
        var json = """[{"ticker":"RY.TO","name":"Royal Bank","sector":"Financials"},{"ticker":"AC.TO","name":"Air Canada","sector":"Industrials"}]""";
        _sdk.ReadResourceAsync("tsx://companies/list", default)
            .Returns(ResourceResult(json));

        var result = await _sut.GetCompanyListAsync();

        Assert.Equal(2, result.Count);
        Assert.Equal("RY.TO", result[0].Ticker);
        Assert.Equal("Royal Bank", result[0].Name);
        Assert.Equal("Financials", result[0].Sector);
        await _sdk.Received(1).ReadResourceAsync("tsx://companies/list", default);
    }

    // ── GetCompanyProfileAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetCompanyProfileAsync_KnownTicker_MapsToProfile()
    {
        var json = """{"ticker":"RY.TO","name":"Royal Bank","sector":"Financials","industry":"Diversified Banks","fetched_at":"2024-01-01"}""";
        _sdk.ReadResourceAsync("tsx://ticker/RY.TO/profile", default)
            .Returns(ResourceResult(json));

        var result = await _sut.GetCompanyProfileAsync("RY.TO");

        Assert.NotNull(result);
        Assert.Equal("RY.TO", result.Ticker);
        Assert.Equal("Royal Bank", result.Name);
        Assert.Equal("Diversified Banks", result.Industry);
        Assert.Equal("2024-01-01", result.FetchedAt);
        await _sdk.Received(1).ReadResourceAsync("tsx://ticker/RY.TO/profile", default);
    }

    [Fact]
    public async Task GetCompanyProfileAsync_UnknownTicker_ReturnsNull()
    {
        _sdk.ReadResourceAsync("tsx://ticker/UNKNOWN.TO/profile", default)
            .ThrowsAsync(new McpException("Resource not found"));

        var result = await _sut.GetCompanyProfileAsync("UNKNOWN.TO");

        Assert.Null(result);
    }

    // ── SummarizeQuarterAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task SummarizeQuarterAsync_CallsCorrectTool_AndMapsText()
    {
        const string summaryText = "Royal Bank Q4 2023: Revenue $15B, Net Income $4B.";
        _sdk.CallToolAsync(
                "summarize_quarter",
                Arg.Is<IReadOnlyDictionary<string, object?>>(d => d.ContainsKey("ticker") && d["ticker"]!.ToString() == "RY.TO"),
                default)
            .Returns(ToolResult(summaryText));

        var result = await _sut.SummarizeQuarterAsync("RY.TO");

        Assert.Equal("RY.TO", result.Ticker);
        Assert.Equal(summaryText, result.Summary);
    }

    // ── SearchFilingsAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task SearchFilingsAsync_WithAllArgs_CallsCorrectTool_AndMapsText()
    {
        const string resultsText = "Q4 2023 filing: Total Revenue $15B...";
        _sdk.CallToolAsync(
                "search_filings",
                Arg.Is<IReadOnlyDictionary<string, object?>>(d =>
                    d["query"]!.ToString() == "revenue" &&
                    d["company"]!.ToString() == "RY.TO" &&
                    d["year"]!.Equals(2023)),
                default)
            .Returns(ToolResult(resultsText));

        var result = await _sut.SearchFilingsAsync("revenue", "RY.TO", 2023);

        Assert.Equal(resultsText, result.Results);
    }

    [Fact]
    public async Task SearchFilingsAsync_QueryOnly_DoesNotIncludeOptionalArgs()
    {
        _sdk.CallToolAsync(
                "search_filings",
                Arg.Is<IReadOnlyDictionary<string, object?>>(d =>
                    d.ContainsKey("query") && !d.ContainsKey("company") && !d.ContainsKey("year")),
                default)
            .Returns(ToolResult("some results"));

        var result = await _sut.SearchFilingsAsync("revenue");

        Assert.Equal("some results", result.Results);
    }
}
