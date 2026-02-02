using BrightnessSensor.App.Models;

namespace BrightnessSensor.App.Domain;

// Converts raw ADC values to target brightness using normalize + EMA + hysteresis.
internal sealed class BrightnessProcessor
{
    private readonly ProcessingSettings _processingSettings;
    private readonly BrightnessSettings _brightnessSettings;
    private double? _emaValue;
    private int? _lastAppliedBrightness;

    public BrightnessProcessor(
        ProcessingSettings processingSettings,
        BrightnessSettings brightnessSettings)
    {
        _processingSettings = processingSettings;
        _brightnessSettings = brightnessSettings;
    }

    public EvaluationResult Evaluate(int rawAdcValue)
    {
        var clampedAdcValue = Math.Clamp(
            rawAdcValue,
            _processingSettings.AdcMin,
            _processingSettings.AdcMax);

        var normalized = (clampedAdcValue - _processingSettings.AdcMin) /
            (double)(_processingSettings.AdcMax - _processingSettings.AdcMin);

        if (_processingSettings.Invert)
        {
            normalized = 1.0 - normalized;
        }

        _emaValue ??= normalized;
        _emaValue = (_processingSettings.EmaAlpha * normalized) +
            ((1.0 - _processingSettings.EmaAlpha) * _emaValue.Value);

        var targetBrightness = (int)Math.Round(
            _brightnessSettings.MinPercent +
            (_emaValue.Value * (_brightnessSettings.MaxPercent - _brightnessSettings.MinPercent)),
            MidpointRounding.AwayFromZero);

        targetBrightness = Math.Clamp(
            targetBrightness,
            _brightnessSettings.MinPercent,
            _brightnessSettings.MaxPercent);

        if (_lastAppliedBrightness.HasValue &&
            Math.Abs(targetBrightness - _lastAppliedBrightness.Value) <
            _processingSettings.HysteresisPercent)
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
