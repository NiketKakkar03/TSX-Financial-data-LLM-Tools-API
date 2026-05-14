using TsxApi.McpClient.Models;

namespace TsxApi.McpClient;

public interface IMcpClient
{
    Task<IReadOnlyList<CompanyListItem>> GetCompanyListAsync(CancellationToken ct = default);
    Task<CompanyProfile?> GetCompanyProfileAsync(string ticker, CancellationToken ct = default);
    Task<QuarterSummary> SummarizeQuarterAsync(string ticker, CancellationToken ct = default);
    Task<FilingSearchResult> SearchFilingsAsync(string query, string? company = null, int? year = null, CancellationToken ct = default);
}
