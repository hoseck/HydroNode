using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace HydroNode.Monitoring
{
    internal class ServerMonitor
    {
        private readonly ILogger _logger;
        private readonly Timer _timer;

        public ServerMonitor(ILogger logger)
        {
            _logger = logger;
#if DEBUG
            _timer = new Timer(async _ => await LogStatusAsync(), null, 0, 1000 * 10); // 10초마다
#else
            _timer = new Timer(async _ => await LogStatusAsync(), null, 0, 1000 * 60 * 60); // 1시간마다
#endif
        }

        private async Task LogStatusAsync()
        {
            try
            {
                float cpu = await GetCpuUsageAsync();
                float mem = GetMemoryUsagePercent();
                _logger.LogInformation($"[모니터링] {DateTime.Now:yyyy-MM-dd HH:mm:ss} CPU 사용률: {cpu:F1}% / 메모리 사용률: {mem:F1}%");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[모니터링] 상태 점검 중 예외 발생");
            }
        }

        public static async Task<float> GetCpuUsageAsync()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return await GetWindowsCpuUsageAsync();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await GetLinuxCpuUsageAsync();
            }

            throw new PlatformNotSupportedException("지원되지 않는 운영체제입니다.");
        }

        public static float GetMemoryUsagePercent()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemoryUsage();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemoryUsage();
            }

            throw new PlatformNotSupportedException("지원되지 않는 운영체제입니다.");
        }

        // ------------------------
        // WINDOWS
        // ------------------------

        private static async Task<float> GetWindowsCpuUsageAsync()
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "-Command \"(Get-Counter '\\Processor(_Total)\\% Processor Time').CounterSamples.CookedValue\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (float.TryParse(output.Trim(), out float cpu))
                return cpu;

            return 0f;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        private static float GetWindowsMemoryUsage()
        {
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                ulong total = memStatus.ullTotalPhys;
                ulong available = memStatus.ullAvailPhys;
                return 100f * (1f - (float)available / total);
            }

            return 0f;
        }

        // ------------------------
        // LINUX
        // ------------------------

        private static async Task<float> GetLinuxCpuUsageAsync()
        {
            string[] cpu1 = await File.ReadAllLinesAsync("/proc/stat");
            await Task.Delay(500);
            string[] cpu2 = await File.ReadAllLinesAsync("/proc/stat");

            var (idle1, total1) = ParseLinuxCpu(cpu1[0]);
            var (idle2, total2) = ParseLinuxCpu(cpu2[0]);

            return 100f * (1f - (idle2 - idle1) / (total2 - total1));
        }

        private static (float idle, float total) ParseLinuxCpu(string line)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var values = parts.Skip(1).Select(float.Parse).ToArray();

            float idle = values[3];
            float iowait = values.Length > 4 ? values[4] : 0;
            float total = values.Sum();

            return (idle + iowait, total);
        }

        private static float GetLinuxMemoryUsage()
        {
            var lines = File.ReadAllLines("/proc/meminfo");

            float total = 0, available = 0;

            foreach (var line in lines)
            {
                if (line.StartsWith("MemTotal:"))
                    total = float.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
                else if (line.StartsWith("MemAvailable:"))
                    available = float.Parse(line.Split(' ', StringSplitOptions.RemoveEmptyEntries)[1]);
            }

            return 100f * (1f - available / total);
        }
    }
}
