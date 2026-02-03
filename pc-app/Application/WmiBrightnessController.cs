using System.Management;

namespace BrightnessSensor.App.Application;

// Windows-specific brightness writer based on WMI built-in display APIs.
internal sealed class WmiBrightnessController
{
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
}
