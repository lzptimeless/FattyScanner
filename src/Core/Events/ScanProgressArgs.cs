using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FattyScanner.Core.Models;

namespace FattyScanner.Core.Events
{
    /// <summary>
    /// 扫描进度事件参数
    /// </summary>
    public class ScanProgressArgs : EventArgs
    {
        public ScanProgressArgs(double progress, ulong scannedSize)
        {
            ProgressValue = progress;
            ScannedSize = scannedSize;
        }

        /// <summary>
        /// 当前扫描进度，0到1的浮点数
        /// </summary>
        public double ProgressValue { get; private set; }
        public ulong ScannedSize { get; private set; }
    }
}
