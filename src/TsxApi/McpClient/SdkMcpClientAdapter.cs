using ModelContextProtocol.Protocol;
using SdkClient = ModelContextProtocol.Client.McpClient;

namespace TsxApi.McpClient;

internal sealed class SdkMcpClientAdapter : ISdkMcpClientAdapter
{
    private readonly SdkClient _client;

    public SdkMcpClientAdapter(SdkClient client) => _client = client;

    public Task<ReadResourceResult> ReadResourceAsync(string uri, CancellationToken ct = default)
        => _client.ReadResourceAsync(uri, null, ct).AsTask();

    public Task<CallToolResult> CallToolAsync(string name, IReadOnlyDictionary<string, object?>? arguments = null, CancellationToken ct = default)
        => _client.CallToolAsync(name, arguments, null, null, ct).AsTask();
}
