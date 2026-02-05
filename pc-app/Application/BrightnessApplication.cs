using System.IO.Ports;
using BrightnessSensor.App.Configuration;
using BrightnessSensor.App.Protocol;

namespace BrightnessSensor.App.Application;

// Orchestrates the app flow: config load, serial read loop, processing, and brightness updates.
internal static class BrightnessApplication
{
    public static int Run(AppConfig config)
    {
        using var serialPort = new SerialPort(config.Serial.PortName, config.Serial.BaudRate);
        serialPort.NewLine = "\n";
        serialPort.ReadTimeout = 1500;

        try
        {
            serialPort.Open();
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Failed to open COM port: {exception.Message}");
            return 1;
        }

        using var cancellationTokenSource = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        Console.CancelKeyPress += handler;
        
        try
        {
            Console.WriteLine($"Port opened: {config.Serial.PortName} @ {config.Serial.BaudRate}");
            Console.WriteLine("Running. Press Ctrl+C to stop.");

            var brightnessProcessor = new BrightnessProcessor(config.Processing, config.Brightness);
            var brightnessController = new CompositeBrightnessController(
                new WmiBrightnessController(),
                new DdcBrightnessController());

            brightnessController.LogDetectedMonitors();

            TryStartupCalibration(serialPort, brightnessProcessor, brightnessController, config.Calibration);

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                var readStatus = TryReadLine(serialPort, out var line, out var readError);
                if (readStatus == ReadStatus.TimeoutOrEmpty)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (readStatus == ReadStatus.Error)
                {
                    Console.Error.WriteLine($"COM read error: {readError}");
                    return 1;
                }

                if (!SensorMessageParser.TryParse(line!, out var sensorMessage))
                {
                    Console.WriteLine($"Skipping invalid JSON: {line}");
                    continue;
                }

                var evaluationResult = brightnessProcessor.Evaluate(sensorMessage.Value);
                if (!evaluationResult.ShouldApply)
                {
                    continue;
                }

                if (!brightnessController.TrySetBrightness(evaluationResult.TargetBrightness, out var error))
                {
                    Console.Error.WriteLine($"Brightness update failed: {error}");
                    continue;
                }

                Console.WriteLine(
                    $"[{DateTime.Now:HH:mm:ss}] raw={sensorMessage.Value,4} norm={evaluationResult.Normalized:F3} filt={evaluationResult.Filtered:F3} -> brightness={evaluationResult.TargetBrightness}%");
            }
        }
        finally
        {
            Console.CancelKeyPress -= handler;
            serialPort.Close();
        }

        return 0;
    }

    private static void TryStartupCalibration(
        SerialPort serialPort,
        BrightnessProcessor brightnessProcessor,
        IBrightnessController brightnessController,
        CalibrationSettings calibrationSettings)
    {
        if (!calibrationSettings.Enabled)
        {
            Console.WriteLine("Startup calibration disabled.");
            return;
        }

        if (!brightnessController.TryGetBrightness(out var currentBrightness, out var brightnessError))
        {
            Console.WriteLine($"Startup calibration skipped: cannot read current brightness ({brightnessError}).");
            return;
        }

        var samples = new List<int>(calibrationSettings.SampleCount);
        var attempts = 0;

        while (attempts < calibrationSettings.MaxReadAttempts &&
            samples.Count < calibrationSettings.SampleCount)
        {
            attempts++;

            var readStatus = TryReadLine(serialPort, out var line, out var readError);
            if (readStatus == ReadStatus.TimeoutOrEmpty)
            {
                continue;
            }
            if (readStatus == ReadStatus.Error)
            {
                Console.WriteLine($"Startup calibration skipped: COM read error ({readError}).");
                return;
            }

            if (!SensorMessageParser.TryParse(line!, out var sensorMessage))
            {
                continue;
            }

            samples.Add(sensorMessage.Value);
        }

        if (samples.Count == 0)
        {
            Console.WriteLine("Startup calibration skipped: no valid sensor data received.");
            return;
        }

        if (samples.Count < calibrationSettings.SampleCount)
        {
            Console.WriteLine(
                $"Startup calibration skipped: not enough samples ({samples.Count}/{calibrationSettings.SampleCount}).");
            return;
        }

        var averageSample = (int)Math.Round(samples.Average(), MidpointRounding.AwayFromZero);

        if (!brightnessProcessor.TryCalibrate(averageSample, currentBrightness, out var error))
        {
            Console.WriteLine($"Startup calibration skipped: {error}");
            return;
        }

        Console.WriteLine(
            $"Startup calibration: screen={currentBrightness}% sensorAvg={averageSample} ({samples.Count} samples)");
    }

    private static ReadStatus TryReadLine(
        SerialPort serialPort,
        out string? line,
        out string? error)
    {
        try
        {
            line = serialPort.ReadLine().Trim();
            error = null;
            return string.IsNullOrWhiteSpace(line) ? ReadStatus.TimeoutOrEmpty : ReadStatus.Success;
        }
        catch (TimeoutException)
        {
            line = null;
            error = null;
            return ReadStatus.TimeoutOrEmpty;
        }
        catch (Exception exception)
        {
            line = null;
            error = exception.Message;
            return ReadStatus.Error;
        }
    }

    private enum ReadStatus
    {
        Success,
        TimeoutOrEmpty,
        Error
    }
}
