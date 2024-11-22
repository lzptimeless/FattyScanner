using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Common
{
    internal static class PerformanceHelper
    {
        #region CPU usage
        // FILETIME 结构
        [StructLayout(LayoutKind.Sequential)]
        struct FILETIME
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

        // 导入 GetProcessTimes API
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetProcessTimes(
            IntPtr hProcess,
            out FILETIME lpCreationTime,
            out FILETIME lpExitTime,
            out FILETIME lpKernelTime,
            out FILETIME lpUserTime
        );

        // 计算 FILETIME 差值
        static ulong SubtractTimes(FILETIME endTime, FILETIME startTime)
        {
            ulong end = ((ulong)endTime.HighDateTime << 32) | endTime.LowDateTime;
            ulong start = ((ulong)startTime.HighDateTime << 32) | startTime.LowDateTime;
            return end - start;
        }

        static FILETIME _preKernelTime, _preUserTime;
        static DateTime _preCpuDataTime;

        public static double GetCpuUsage(IntPtr hProcess)
        {
            FILETIME creationTime, exitTime, kernelTime, userTime;
            if (!GetProcessTimes(hProcess, out creationTime, out exitTime, out kernelTime, out userTime))
                return 0;

            if (_preCpuDataTime == DateTime.MinValue)
            {
                _preCpuDataTime = DateTime.Now;
                _preKernelTime = kernelTime;
                _preUserTime = userTime;
                return 0;
            }

            ulong kernelDiff = SubtractTimes(kernelTime, _preKernelTime);
            ulong userDiff = SubtractTimes(userTime, _preUserTime);
            ulong totalDiff = kernelDiff + userDiff;
            DateTime now = DateTime.Now;
            double cpuUsage = totalDiff / (Environment.ProcessorCount * (now - _preCpuDataTime).TotalMilliseconds * 10000);
            _preCpuDataTime = now;
            _preKernelTime = kernelTime;
            _preUserTime = userTime;

            return Math.Round(cpuUsage, 2);
        }
        #endregion

        #region RAM usage
        // P/Invoke declaration for K32GetProcessMemoryInfo function
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool K32GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS counters, uint size);

        // Memory counters structure
        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_MEMORY_COUNTERS
        {
            public uint cb;
            public uint PageFaultCount;
            public uint PeakWorkingSetSize;
            public uint WorkingSetSize;
            public uint QuotaPeakPagedPoolUsage;
            public uint QuotaPagedPoolUsage;
            public uint QuotaPeakNonPagedPoolUsage;
            public uint QuotaNonPagedPoolUsage;
            public uint PagefileUsage;
            public uint PeakPagefileUsage;
        }

        public static long GetRamUsage(IntPtr hProcess)
        {
            PROCESS_MEMORY_COUNTERS pmc;
            pmc.cb = (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS));
            if (!K32GetProcessMemoryInfo(hProcess, out pmc, pmc.cb))
            {
                throw new Win32Exception();
            }

            return pmc.WorkingSetSize;
        }
        #endregion
    }
}
