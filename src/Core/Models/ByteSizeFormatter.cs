using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Core.Models
{
    internal static class ByteSizeFormatter
    {
        private static readonly string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        /// <summary>
        /// 将字节大小转化为字符串表示
        /// </summary>
        /// <param name="value">字节大小</param>
        /// <returns></returns>
        public static string SizeSuffix(long value)
        {
            if (value < 0) { return "-" + SizeSuffix(-value); }
            if (value == 0) { return $"0{SizeSuffixes[0]}"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = Math.Round((decimal)value / (1L << (mag * 10)), 1);

            return string.Format("{0}{1}", adjustedSize, SizeSuffixes[mag]);
        }
    }
}
