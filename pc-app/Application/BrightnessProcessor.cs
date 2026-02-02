using BrightnessSensor.App.Configuration;

namespace BrightnessSensor.App.Application;

// Converts raw ADC values to target brightness using normalize + EMA + hysteresis.
internal sealed class BrightnessProcessor(
    ProcessingSettings processingSettings,
    BrightnessSettings brightnessSettings)
{
    private double? _emaValue;
    private int? _lastAppliedBrightness;

    public EvaluationResult Evaluate(int rawAdcValue)
    {
        var clampedAdcValue = Math.Clamp(
            rawAdcValue,
            processingSettings.AdcMin,
            processingSettings.AdcMax);

        var normalized = (clampedAdcValue - processingSettings.AdcMin) /
            (double)(processingSettings.AdcMax - processingSettings.AdcMin);

        if (processingSettings.Invert)
        {
            normalized = 1.0 - normalized;
        }

        _emaValue ??= normalized;
        _emaValue = (processingSettings.EmaAlpha * normalized) +
            ((1.0 - processingSettings.EmaAlpha) * _emaValue.Value);

        var targetBrightness = (int)Math.Round(
            brightnessSettings.MinPercent +
            (_emaValue.Value * (brightnessSettings.MaxPercent - brightnessSettings.MinPercent)),
            MidpointRounding.AwayFromZero);

        targetBrightness = Math.Clamp(
            targetBrightness,
            brightnessSettings.MinPercent,
            brightnessSettings.MaxPercent);

        if (_lastAppliedBrightness.HasValue &&
            Math.Abs(targetBrightness - _lastAppliedBrightness.Value) <
            processingSettings.HysteresisPercent)
        {
            return new EvaluationResult(
                ShouldApply: false,
                TargetBrightness: targetBrightness,
                Normalized: normalized,
                Filtered: _emaValue.Value);
        }

        _lastAppliedBrightness = targetBrightness;
        return new EvaluationResult(
            ShouldApply: true,
            TargetBrightness: targetBrightness,
            Normalized: normalized,
            Filtered: _emaValue.Value);
    }
}
