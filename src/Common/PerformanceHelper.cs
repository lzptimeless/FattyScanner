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

            return Math.Round(cpuUsage, 4);
        }
        #endregion

        #region RAM usage
        // P/Invoke declaration for K32GetProcessMemoryInfo function
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool K32GetProcessMemoryInfo(IntPtr hProcess, out PROCESS_MEMORY_COUNTERS_EX counters, uint size);

        // Memory counters structure
        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_MEMORY_COUNTERS_EX
        {
            public uint cb;                       // 结构体大小
            public uint PageFaultCount;           // 页面错误数
            public ulong PeakWorkingSetSize;      // 峰值工作集大小
            public ulong WorkingSetSize;          // 当前工作集大小
            public ulong QuotaPeakPagedPoolUsage; // 峰值分页池使用量
            public ulong QuotaPagedPoolUsage;     // 分页池使用量
            public ulong QuotaPeakNonPagedPoolUsage; // 峰值非分页池使用量
            public ulong QuotaNonPagedPoolUsage;  // 非分页池使用量
            public ulong PagefileUsage;           // 页面文件使用量
            public ulong PeakPagefileUsage;       // 页面文件峰值使用量
            public ulong PrivateUsage;            // 私有字节数
        }

        public static ulong GetRamUsage(IntPtr hProcess)
        {
            PROCESS_MEMORY_COUNTERS_EX pmc;
            pmc.cb = (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS_EX));
            if (!K32GetProcessMemoryInfo(hProcess, out pmc, pmc.cb))
            {
                throw new Win32Exception();
            }

            return pmc.PrivateUsage;
        }
        #endregion

        #region Disk uspage
        [StructLayout(LayoutKind.Sequential)]
        struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS ioCounters);

        static IO_COUNTERS _preIoCounters;

        public static ulong GetDiskUsage(IntPtr hProcess)
        {
            IO_COUNTERS ioCounters;
            if (!GetProcessIoCounters(hProcess, out ioCounters))
            {
                throw new Win32Exception();
            }

            ulong readTransferCount = ioCounters.ReadTransferCount;
            ulong writeTransferCount = ioCounters.WriteTransferCount;
            ulong totalTransferCount = readTransferCount + writeTransferCount;
            ulong diskUsage = totalTransferCount - _preIoCounters.ReadTransferCount - _preIoCounters.WriteTransferCount;
            _preIoCounters = ioCounters;
            return diskUsage;
        }
        #endregion
    }
}
