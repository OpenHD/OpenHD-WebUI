namespace OpenHdWebUi.Server.Models;

public sealed class RfControlRequest
{
    public string? InterfaceName { get; set; }
    public int? FrequencyMhz { get; set; }
    public int? ChannelWidthMhz { get; set; }
    public int? McsIndex { get; set; }
    public int? TxPowerMw { get; set; }
    public int? TxPowerIndex { get; set; }
    public string? PowerLevel { get; set; }
}

public sealed class RfControlResponse
{
    public bool Ok { get; set; }
    public string? Message { get; set; }
}
