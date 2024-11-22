using FattyScanner.Core.Events;
using FattyScanner.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Core
{
    /// <summary>
    /// 扫描功能接口
    /// </summary>
    public interface IScanModule
    {
        ScanStates ScanState { get; }
        string? ScanPath { get; }
        FileSysNode? ScanResult { get; }

        event EventHandler<ScanProgressArgs>? ScanProgressChanged;
        event EventHandler<ScanStateChangedArgs>? ScanStateChanged;

        void StartScan(string path);
        void StopScan();
        void Expand(FileSysNode node);
    }
}
