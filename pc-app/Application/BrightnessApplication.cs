using System.IO.Ports;
using BrightnessSensor.App.Configuration;
using BrightnessSensor.App.Platform.Windows;
using BrightnessSensor.App.Protocol;

namespace BrightnessSensor.App.Application;

// Orchestrates the app flow: config load, serial read loop, processing, and brightness updates.
internal static class BrightnessApplication
{
    public static int Run(string[] args)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.Error.WriteLine("This application supports Windows only.");
            return 1;
        }

        var loadResult = TryLoadConfig(args, out var config, out var configError);
        if (!loadResult)
        {
            Console.Error.WriteLine($"Configuration error: {configError}");
            return 1;
        }

        using var serialPort = new SerialPort(config!.Serial.PortName, config.Serial.BaudRate);
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
        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationTokenSource.Cancel();
        };

        Console.WriteLine($"Port opened: {config.Serial.PortName} @ {config.Serial.BaudRate}");
        Console.WriteLine("Running. Press Ctrl+C to stop.");

        var brightnessProcessor = new BrightnessProcessor(config.Processing, config.Brightness);
        var brightnessController = new WmiBrightnessController();

        while (!cancellationTokenSource.IsCancellationRequested)
        {
            var readStatus = TryReadLine(serialPort, out var line, out var readError);
            if (readStatus == ReadStatus.TimeoutOrEmpty)
            {
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

        return 0;
    }

    private static bool TryLoadConfig(string[] args, out AppConfig? config, out string error)
    {
        try
        {
            var configPath = args.Length > 0 ? args[0] : AppConfigLoader.ResolveDefaultPath();
            config = AppConfigLoader.Load(configPath);
            error = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            config = null;
            error = exception.Message;
            return false;
        }
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
