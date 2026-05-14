namespace TsxApi.Configuration;

public sealed class McpServerOptions
{
    public const string SectionName = "McpServer";

    public string McpServerUrl { get; set; } = "http://localhost:8000";
    public int TimeoutSeconds { get; set; } = 30;
}
