namespace BrightnessSensor.App.Application;

internal sealed class CompositeBrightnessController(params IBrightnessController[] controllers) : IBrightnessController
{
    public void LogDetectedMonitors()
    {
        foreach (var controller in controllers)
        {
            controller.LogDetectedMonitors();
        }
    }

    public bool TryGetBrightness(out int brightnessPercent, out string? error)
    {
        var errors = new List<string>();

        foreach (var controller in controllers)
        {
            if (controller.TryGetBrightness(out brightnessPercent, out error))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                errors.Add(error);
            }
        }

        brightnessPercent = 0;
        error = errors.Count == 0 ? "No brightness controllers available." : string.Join(" | ", errors);
        return false;
    }

    public bool TrySetBrightness(int brightnessPercent, out string? error)
    {
        var anySuccess = false;

        foreach (var controller in controllers)
        {
            if (controller.TrySetBrightness(brightnessPercent, out _))
            {
                anySuccess = true;
            }
        }

        if (anySuccess)
        {
            error = null;
            return true;
        }

        error = "No brightness controllers available or all updates failed.";
        return false;
    }
}
