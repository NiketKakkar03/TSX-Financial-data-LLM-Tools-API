using System.Text.Json.Serialization;

namespace TsxApi.McpClient.Models;

public sealed record CompanyProfile(
    string Ticker,
    string Name,
    string Sector,
    string Industry,
    [property: JsonPropertyName("fetched_at")] string FetchedAt
);
