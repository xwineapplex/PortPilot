using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using PortPilot_Project.Abstractions;
using PortPilot_Project.Properties;

namespace PortPilot_Project.Windows;

public sealed class WinMonitorController : IMonitorController
{
    public Task<IReadOnlyList<MonitorInfo>> GetMonitorsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
            return Task.FromResult<IReadOnlyList<MonitorInfo>>(Array.Empty<MonitorInfo>());

        var result = new List<MonitorInfo>();

        Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr _, ref Native.RECT rc, IntPtr __) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!Native.GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, out var count) || count == 0)
                return true;

            var physical = new Native.PHYSICAL_MONITOR[count];
            if (!Native.GetPhysicalMonitorsFromHMONITOR(hMon, count, physical))
                return true;

            try
            {
                for (var i = 0; i < physical.Length; i++)
                {
                    var pm = physical[i];
                    var id = $"{hMon.ToString("X")}:{i}";
                    var name = pm.GetDescription();
                    result.Add(new MonitorInfo(id, name));
                }
            }
            finally
            {
                Native.DestroyPhysicalMonitors(count, physical);
            }

            return true;
        }, IntPtr.Zero);

        return Task.FromResult<IReadOnlyList<MonitorInfo>>(result);
    }

    public Task SetInputSourceAsync(string monitorId, ushort sourceCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
            return Task.CompletedTask;

        // Use monitorId format "<HMONITOR_HEX>:<index>" from GetMonitorsAsync.
        if (!TryParseMonitorId(monitorId, out var hMonitorHex, out var physicalIndex))
            throw new ArgumentException(Resources.Msg_Error_InvalidMonitorIdFormat, nameof(monitorId));

        Native.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMon, IntPtr _, ref Native.RECT rc, IntPtr __) =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!string.Equals(hMon.ToString("X"), hMonitorHex, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!Native.GetNumberOfPhysicalMonitorsFromHMONITOR(hMon, out var count) || count == 0)
                return true;

            var physical = new Native.PHYSICAL_MONITOR[count];
            if (!Native.GetPhysicalMonitorsFromHMONITOR(hMon, count, physical))
                return true;

            try
            {
                if (physicalIndex < 0 || physicalIndex >= physical.Length)
                    throw new ArgumentOutOfRangeException(nameof(monitorId), Resources.Msg_Error_PhysicalMonitorIndexOutOfRange);

                var pm = physical[physicalIndex];

                // Use VCP code 0x60 for Input Source.
                if (!Native.SetVCPFeature(pm.hPhysicalMonitor, 0x60, sourceCode))
                    throw new Win32Exception(Marshal.GetLastWin32Error());

                return false; // Stop enumeration.
            }
            finally
            {
                Native.DestroyPhysicalMonitors(count, physical);
            }
         }, IntPtr.Zero);

        return Task.CompletedTask;
    }

    private static bool TryParseMonitorId(string monitorId, out string hMonitorHex, out int physicalIndex)
    {
        hMonitorHex = string.Empty;
        physicalIndex = -1;
        if (string.IsNullOrWhiteSpace(monitorId))
            return false;

        var parts = monitorId.Split(':', 2);
        if (parts.Length != 2)
            return false;

        hMonitorHex = parts[0];
        return int.TryParse(parts[1], out physicalIndex);
    }

    private static class Native
    {
        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;

            public string GetDescription()
            {
                // Normalize trailing nulls from some drivers.
                return (szPhysicalMonitorDescription ?? string.Empty).TrimEnd('\0').Trim();
            }
        }

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, out uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", SetLastError = true)]
        public static extern bool SetVCPFeature(IntPtr hMonitor, byte bVCPCode, ushort wNewValue);
    }
}
