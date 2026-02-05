using System.Runtime.InteropServices;

namespace BrightnessSensor.App.Application;

// External monitor brightness control via DDC/CI (dxva2 + user32).
internal sealed class DdcBrightnessController : IBrightnessController
{
    private static bool _loggedMonitors;

    public void LogDetectedMonitors()
    {
        if (!TryGetPhysicalMonitors(out var monitors, out _))
        {
            LogMonitorsOnce(Array.Empty<PHYSICAL_MONITOR>());
            return;
        }

        try
        {
            LogMonitorsOnce(monitors);
        }
        finally
        {
            DestroyPhysicalMonitors((uint)monitors.Length, monitors);
        }
    }

    public bool TryGetBrightness(out int brightnessPercent, out string? error)
    {
        brightnessPercent = 0;
        error = null;

        if (!TryGetPhysicalMonitors(out var monitors, out error))
        {
            return false;
        }

        try
        {
            foreach (var monitor in monitors)
            {
                if (GetMonitorBrightness(
                        monitor.hPhysicalMonitor,
                        out var min,
                        out var current,
                        out var max))
                {
                    brightnessPercent = NormalizeToPercent(current, min, max);
                    return true;
                }
            }

            error = "No DDC/CI monitor reported brightness.";
            return false;
        }
        finally
        {
            DestroyPhysicalMonitors((uint)monitors.Length, monitors);
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

        if (!TryGetPhysicalMonitors(out var monitors, out error))
        {
            return false;
        }

        var updatedAny = false;

        try
        {
            foreach (var monitor in monitors)
            {
                if (!GetMonitorBrightness(
                        monitor.hPhysicalMonitor,
                        out var min,
                        out var _,
                        out var max))
                {
                    if (SetMonitorBrightness(monitor.hPhysicalMonitor, (uint)brightnessPercent))
                    {
                        updatedAny = true;
                    }
                    continue;
                }

                var targetValue = MapPercentToMonitorValue(brightnessPercent, min, max);
                if (SetMonitorBrightness(monitor.hPhysicalMonitor, targetValue))
                {
                    updatedAny = true;
                }
            }
        }
        finally
        {
            DestroyPhysicalMonitors((uint)monitors.Length, monitors);
        }

        if (!updatedAny)
        {
            error = "No DDC/CI brightness-capable external monitor found.";
            return false;
        }

        return true;
    }

    private static void LogMonitorsOnce(PHYSICAL_MONITOR[] monitors)
    {
        if (_loggedMonitors)
        {
            return;
        }

        _loggedMonitors = true;

        if (monitors.Length == 0)
        {
            Console.WriteLine("DDC/CI: no external monitors detected.");
            return;
        }

        Console.WriteLine($"DDC/CI: detected {monitors.Length} external monitor(s):");
        foreach (var monitor in monitors)
        {
            var description = string.IsNullOrWhiteSpace(monitor.szPhysicalMonitorDescription)
                ? "<unknown>"
                : monitor.szPhysicalMonitorDescription.Trim();
            Console.WriteLine($"DDC/CI: - {description}");
        }
    }

    private static int NormalizeToPercent(uint value, uint min, uint max)
    {
        if (max <= min)
        {
            return 0;
        }

        var normalized = (value - min) / (double)(max - min);
        return (int)Math.Round(normalized * 100, MidpointRounding.AwayFromZero);
    }

    private static uint MapPercentToMonitorValue(int percent, uint min, uint max)
    {
        if (max <= min)
        {
            return min;
        }

        var normalized = percent / 100.0;
        var value = min + (uint)Math.Round(normalized * (max - min), MidpointRounding.AwayFromZero);

        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }

    private static bool TryGetPhysicalMonitors(out PHYSICAL_MONITOR[] monitors, out string? error)
    {
        var list = new List<PHYSICAL_MONITOR>();
        error = null;

        var result = EnumDisplayMonitors(
            IntPtr.Zero,
            IntPtr.Zero,
            (hMonitor, _, _, _) =>
            {
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, out var count) || count == 0)
                {
                    return true;
                }

                var buffer = new PHYSICAL_MONITOR[count];
                if (GetPhysicalMonitorsFromHMONITOR(hMonitor, count, buffer))
                {
                    list.AddRange(buffer);
                }

                return true;
            },
            IntPtr.Zero);

        if (!result)
        {
            monitors = Array.Empty<PHYSICAL_MONITOR>();
            error = "EnumDisplayMonitors failed.";
            return false;
        }

        if (list.Count == 0)
        {
            monitors = Array.Empty<PHYSICAL_MONITOR>();
            error = "No DDC/CI capable external monitor found.";
            return false;
        }

        monitors = list.ToArray();
        return true;
    }

    private delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, IntPtr lprcMonitor, IntPtr dwData);

    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr lprcClip,
        MonitorEnumProc lpfnEnum,
        IntPtr dwData);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        out uint pdwNumberOfPhysicalMonitors);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetPhysicalMonitorsFromHMONITOR(
        IntPtr hMonitor,
        uint dwPhysicalMonitorArraySize,
        [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool DestroyPhysicalMonitors(
        uint dwPhysicalMonitorArraySize,
        [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool GetMonitorBrightness(
        IntPtr hMonitor,
        out uint pdwMinimumBrightness,
        out uint pdwCurrentBrightness,
        out uint pdwMaximumBrightness);

    [DllImport("dxva2.dll", SetLastError = true)]
    private static extern bool SetMonitorBrightness(
        IntPtr hMonitor,
        uint dwNewBrightness);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct PHYSICAL_MONITOR
    {
        public IntPtr hPhysicalMonitor;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szPhysicalMonitorDescription;
    }
}
