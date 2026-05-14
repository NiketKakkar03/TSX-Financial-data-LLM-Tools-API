using ModelContextProtocol.Protocol;

namespace TsxApi.McpClient;

internal interface ISdkMcpClientAdapter
{
    Task<ReadResourceResult> ReadResourceAsync(string uri, CancellationToken ct = default);
    Task<CallToolResult> CallToolAsync(string name, IReadOnlyDictionary<string, object?>? arguments = null, CancellationToken ct = default);
}
