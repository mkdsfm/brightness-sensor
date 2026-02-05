using System.Management;

namespace BrightnessSensor.App.Application;

// Windows-specific brightness writer based on WMI built-in display APIs.
internal sealed class WmiBrightnessController : IBrightnessController
{
    private static bool _loggedMonitors;

    public void LogDetectedMonitors()
    {
        var names = new List<string>();

        try
        {
            var scope = new ManagementScope(@"\\.\root\wmi");
            scope.Connect();

            using var monitorClass = new ManagementClass(scope, new ManagementPath("WmiMonitorBrightness"), null);
            using var instances = monitorClass.GetInstances();

            foreach (var o in instances)
            {
                var monitor = (ManagementObject) o;
                using (monitor)
                {
                    if (monitor["InstanceName"] is string instanceName && !string.IsNullOrWhiteSpace(instanceName))
                    {
                        names.Add(instanceName.Trim());
                    }
                }
            }
        }
        catch
        {
            // Ignore errors here; LogMonitorsOnce will handle empty list.
        }

        LogMonitorsOnce(names);
    }

    public bool TryGetBrightness(out int brightnessPercent, out string? error)
    {
        error = null;
        brightnessPercent = 0;

        try
        {
            var scope = new ManagementScope(@"\\.\root\wmi");
            scope.Connect();

            using var monitorClass = new ManagementClass(scope, new ManagementPath("WmiMonitorBrightness"), null);
            using var instances = monitorClass.GetInstances();

            foreach (var o in instances)
            {
                var monitor = (ManagementObject) o;
                using (monitor)
                {
                    if (monitor["CurrentBrightness"] is byte current)
                    {
                        brightnessPercent = current;
                        return true;
                    }
                }
            }

            error = "No WMI brightness-capable built-in display found (root\\wmi).";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public bool TrySetBrightness(int brightnessPercent, out string? error)
    {
        error = null;

        if (brightnessPercent is < 0 or > 100)
        {
            error = "brightnessPercent must be in range 0..100";
            return false;
        }

        try
        {
            var scope = new ManagementScope(@"\\.\root\wmi");
            scope.Connect();

            using var monitorClass = new ManagementClass(scope, new ManagementPath("WmiMonitorBrightnessMethods"), null);
            using var instances = monitorClass.GetInstances();

            var updatedAny = false;

            foreach (var o in instances)
            {
                var monitor = (ManagementObject) o;
                using (monitor)
                {
                    // WmiSetBrightness(uint timeout, byte brightness)
                    monitor.InvokeMethod("WmiSetBrightness", [(uint)0, (byte)brightnessPercent]);
                    updatedAny = true;
                }
            }

            if (!updatedAny)
            {
                error = "No WMI brightness-capable built-in display found (root\\wmi).";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void LogMonitorsOnce(List<string> instanceNames)
    {
        if (_loggedMonitors)
        {
            return;
        }

        _loggedMonitors = true;

        if (instanceNames.Count == 0)
        {
            Console.WriteLine("WMI: no built-in brightness-capable displays detected.");
            return;
        }

        Console.WriteLine($"WMI: detected {instanceNames.Count} built-in display(s):");
        foreach (var name in instanceNames.Distinct())
        {
            Console.WriteLine($"WMI: - {name}");
        }
    }
}
