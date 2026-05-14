using Microsoft.AspNetCore.Mvc;
using TsxApi.McpClient;
using TsxApi.Models;

namespace TsxApi.Controllers;

[ApiController]
[Route("api/stocks")]
public sealed class StocksController : ControllerBase
{
    private readonly IMcpClient _mcpClient;
    private readonly ILogger<StocksController> _logger;

    public StocksController(IMcpClient mcpClient, ILogger<StocksController> logger)
    {
        _mcpClient = mcpClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetCompanies(CancellationToken ct)
    {
        _logger.LogInformation("GET /api/stocks");
        try
        {
            var companies = await _mcpClient.GetCompanyListAsync(ct);
            _logger.LogInformation("GET /api/stocks → {Count} companies", companies.Count);
            return Ok(companies);
        }
        catch (Exception ex) when (IsMcpConnectivityError(ex))
        {
            _logger.LogError(ex, "GET /api/stocks → MCP server unreachable");
            return StatusCode(502, new ErrorResponse(502, "MCP server is unreachable."));
        }
    }

    private static bool IsMcpConnectivityError(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or TimeoutException
        || (ex.GetType().FullName?.StartsWith("ModelContextProtocol") == true);
}
