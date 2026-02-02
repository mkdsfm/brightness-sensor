using System.Text.Json.Serialization;

namespace BrightnessSensor.App.Models;

// Target brightness bounds in percent.
internal sealed class BrightnessSettings
{
    [JsonPropertyName("minPercent")]
    public int MinPercent { get; init; }

    [JsonPropertyName("maxPercent")]
    public int MaxPercent { get; init; }
}
