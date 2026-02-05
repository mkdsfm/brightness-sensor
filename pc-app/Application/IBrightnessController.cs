namespace BrightnessSensor.App.Application;

internal interface IBrightnessController
{
    void LogDetectedMonitors();

    bool TryGetBrightness(out int brightnessPercent, out string? error);

    bool TrySetBrightness(int brightnessPercent, out string? error);
}
