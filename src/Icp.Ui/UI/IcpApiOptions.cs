namespace Icp.Ui;

public sealed class IcpApiOptions
{
    public const string SectionName = "IcpApi";

    public string BaseUrl { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;

    public string Environment { get; set; } = "Production";
}
