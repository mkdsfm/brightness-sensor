using System.Text.Json.Serialization;

namespace BrightnessSensor.App.Configuration;

// Serial communication settings for reading telemetry from ESP32.
internal sealed class SerialSettings
{
    [JsonPropertyName("portName")]
    public required string PortName { get; init; }

    [JsonPropertyName("baudRate")]
    public int BaudRate { get; init; } = 115200;
}
