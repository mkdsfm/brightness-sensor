using System.Management;

namespace BrightnessSensor.App.Application;

// Windows-specific brightness writer based on WMI built-in display APIs.
internal sealed class WmiBrightnessController
{
    public bool TrySetBrightness(int brightnessPercent, out string? error)
    {
        try
        {
            using var monitorClass = new ManagementClass("WmiMonitorBrightnessMethods");
            using var monitorInstances = monitorClass.GetInstances();

            var updatedAnyDisplay = false;
            foreach (var monitorObject in monitorInstances)
            {
                var monitor = (ManagementObject)monitorObject;
                using (monitor)
                {
                    monitor.InvokeMethod("WmiSetBrightness", [(uint)0, (byte)brightnessPercent]);
                    updatedAnyDisplay = true;
                }
            }

            if (!updatedAnyDisplay)
            {
                error = "WMI did not return any built-in display.";
                return false;
            }

            error = null;
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            return false;
        }
    }
}
