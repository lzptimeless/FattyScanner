using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FattyScanner.Core.Models;

namespace FattyScanner.Core.Events
{
    /// <summary>
    /// 扫描状态改变事件参数
    /// </summary>
    public class ScanStateChangedArgs : EventArgs
    {
        public ScanStateChangedArgs(ScanStates state)
        {
            State = state;
        }

        /// <summary>
        /// 当前扫描状态
        /// </summary>
        public ScanStates State { get; private set; }
    }
}
