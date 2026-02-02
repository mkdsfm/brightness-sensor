using System.Text.Json.Serialization;

namespace BrightnessSensor.App.Configuration;

// Root configuration model loaded from appsettings.json.
internal sealed class AppConfig
{
    [JsonPropertyName("serial")]
    public required SerialSettings Serial { get; init; }

    [JsonPropertyName("processing")]
    public required ProcessingSettings Processing { get; init; }

    [JsonPropertyName("brightness")]
    public required BrightnessSettings Brightness { get; init; }
}
