namespace TsxApi.Configuration;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    public string ApiKey { get; set; } = string.Empty;
}
