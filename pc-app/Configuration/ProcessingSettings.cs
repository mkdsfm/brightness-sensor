using System.Text.Json.Serialization;

namespace BrightnessSensor.App.Configuration;

// Processing parameters: ADC range, inversion, smoothing, and hysteresis.
internal sealed class ProcessingSettings
{
    [JsonPropertyName("adcMin")]
    public int AdcMin { get; init; }

    [JsonPropertyName("adcMax")]
    public int AdcMax { get; init; }

    [JsonPropertyName("invert")]
    public bool Invert { get; init; }

    [JsonPropertyName("emaAlpha")]
    public double EmaAlpha { get; init; }

    [JsonPropertyName("hysteresisPercent")]
    public int HysteresisPercent { get; init; }
}
