using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Core.Models
{
    /// <summary>
    /// 文件扫描状态
    /// </summary>
    public enum ScanStates
    {
        /// <summary>
        /// 空闲，没有扫描或之前的扫描结果已经被清理
        /// </summary>
        Idle,
        /// <summary>
        /// 正在扫描，可以获取已经扫描的结果
        /// </summary>
        Scanning,
        /// <summary>
        /// 扫描完成，可以获取扫描结果
        /// </summary>
        Completed,
        /// <summary>
        /// 正在清理扫描数据，完成后恢复Idle状态
        /// </summary>
        Cleaning
    }
}
