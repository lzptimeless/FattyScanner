using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Core.Models
{
    /// <summary>
    /// Format bytes to string or parse string to bytes.
    /// </summary>
    public class ByteConverter
    {
        private static readonly string[] FormatSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static string Format(ulong value)
        {
            if (value == 0) { return $"0 {FormatSuffixes[0]}"; }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = Math.Round((decimal)value / (1L << (mag * 10)), 1);

            return string.Format("{0} {1}", adjustedSize, FormatSuffixes[mag]);
        }

        public static bool TryParse(string sizeStr, out long bytes)
        {
            bytes = 0;
            if (string.IsNullOrWhiteSpace(sizeStr))
            {
                return false;
            }

            int suffixIndex = -1;
            string? numStr = null;
            for (int i = 0; i < FormatSuffixes.Length; i++)
            {
                var suffix = FormatSuffixes[i];
                var index = sizeStr.IndexOf(suffix);
                if (index >= 1)
                {
                    suffixIndex = i;
                    numStr = sizeStr.Substring(0, index).Trim();
                    break;
                }
            }

            if (suffixIndex >= 0 && double.TryParse(numStr, out double num))
            {
                bytes = (long)(num * Math.Pow(1024, suffixIndex));
                return true;
            }

            return false;
        }
    }
}
