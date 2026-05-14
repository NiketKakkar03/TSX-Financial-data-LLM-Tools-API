using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using System.Text.Json;
using TsxApi.McpClient.Models;

namespace TsxApi.McpClient;

internal sealed class McpClientService : IMcpClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ISdkMcpClientAdapter _sdk;

    public McpClientService(ISdkMcpClientAdapter sdk) => _sdk = sdk;

    public async Task<IReadOnlyList<CompanyListItem>> GetCompanyListAsync(CancellationToken ct = default)
    {
        var result = await _sdk.ReadResourceAsync("tsx://companies/list", ct);
        var text = ExtractResourceText(result);
        return JsonSerializer.Deserialize<CompanyListItem[]>(text, JsonOpts) ?? [];
    }

    public async Task<CompanyProfile?> GetCompanyProfileAsync(string ticker, CancellationToken ct = default)
    {
        try
        {
            var result = await _sdk.ReadResourceAsync($"tsx://ticker/{ticker}/profile", ct);
            var text = ExtractResourceText(result);
            return JsonSerializer.Deserialize<CompanyProfile>(text, JsonOpts);
        }
        catch (McpException)
        {
            return null;
        }
    }

    public async Task<QuarterSummary> SummarizeQuarterAsync(string ticker, CancellationToken ct = default)
    {
        var result = await _sdk.CallToolAsync(
            "summarize_quarter",
            new Dictionary<string, object?> { { "ticker", ticker } },
            ct);
        return new QuarterSummary(ticker, ExtractToolText(result));
    }

    public async Task<FilingSearchResult> SearchFilingsAsync(
        string query, string? company = null, int? year = null, CancellationToken ct = default)
    {
        var args = new Dictionary<string, object?> { { "query", query } };
        if (company is not null) args["company"] = company;
        if (year.HasValue) args["year"] = year.Value;

        var result = await _sdk.CallToolAsync("search_filings", args, ct);
        return new FilingSearchResult(ExtractToolText(result));
    }

    private static string ExtractResourceText(ReadResourceResult result)
    {
        var content = result.Contents.OfType<TextResourceContents>().FirstOrDefault()
            ?? throw new InvalidOperationException("MCP resource returned no text content.");
        return content.Text ?? string.Empty;
    }

    private static string ExtractToolText(CallToolResult result)
    {
        var content = result.Content.OfType<TextContentBlock>().FirstOrDefault()
            ?? throw new InvalidOperationException("MCP tool returned no text content.");
        return content.Text ?? string.Empty;
    }
}
