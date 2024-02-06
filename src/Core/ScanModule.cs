using FattyScanner.Core.Events;
using FattyScanner.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FattyScanner.Core
{
    public class ScanModule : IScanModule
    {
        #region fields
        /// <summary>
        /// 内部状态锁
        /// </summary>
        private readonly object _lock = new object();
        private readonly ILogger _logger;
        /// <summary>
        /// 管理扫描进度事件触发频率
        /// </summary>
        private readonly ScanProgressEventer _scanProgressEventer;
        /// <summary>
        /// 扫描结果的根目录节点
        /// </summary>
        private ScannedFileSysInfoCompressed? _scannedRoot;
        private CancellationTokenSource? _scanCTS;
        #endregion

        public ScanModule(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(ScanModule));
            _scanProgressEventer = new ScanProgressEventer(0.0001, 300);
            _scanProgressEventer.ProgressChanged += _scanProgressEventer_ProgressChanged;
        }

        #region properties
        public ScanStates ScanState { get; private set; }
        public string? ScanPath { get; private set; }
        #endregion

        #region events
        public event EventHandler<ScanProgressArgs>? ScanProgressChanged;
        private void RaiseScanProgressChanged(ScanProgressArgs e)
        {
            try
            {
                ScanProgressChanged?.Invoke(this, e);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RaiseScanProgressChanged failed, progress: {@e}", e);
            }
        }
        public event EventHandler<ScanStateChangedArgs>? ScanStateChanged;
        private void RaiseScanStateChanged(ScanStates state)
        {
            try
            {
                ScanStateChanged?.Invoke(this, new ScanStateChangedArgs(state));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RaiseScanStateChanged failed, state: {state}", state);
            }
        }
        #endregion

        #region public methods
        public List<string> GetDisks()
        {
            var diskList = new List<string>();
            foreach (var item in DriveInfo.GetDrives())
            {
                diskList.Add(item.Name);
            }
            return diskList;
        }

        public void StartScan(string scanPath)
        {
            if (string.IsNullOrWhiteSpace(scanPath)) throw new ArgumentException("Can not be null or empty", nameof(scanPath));
            if (!Path.IsPathRooted(scanPath)) throw new ArgumentException("Must be a rooted path", nameof(scanPath));

            var scanDirInfo = new DirectoryInfo(scanPath);
            var oldState = ScanStates.Idle;
            CancellationTokenSource cts;
            lock (_lock)
            {
                if (ScanState == ScanStates.Scanning) throw new ApplicationException("Already in scanning state.");
                if (ScanState == ScanStates.Cleaning) throw new ApplicationException("In cleaning state.");

                oldState = ScanState;
                ScanState = ScanStates.Scanning;
                ScanPath = scanPath;
                _scanProgressEventer.Reset();
                _scannedRoot?.CleanSubs();
                _scannedRoot = new ScannedFileSysInfoCompressed
                {
                    IsDir = true,
                    IsFileFilled = true
                };
                _scannedRoot.SetName(scanDirInfo.Name);
                _scanCTS?.Dispose();
                _scanCTS = new CancellationTokenSource();
                cts = _scanCTS;
            }
            RaiseScanStateChanged(ScanStates.Scanning);

            _ = Task.Run(() =>
            {
                try
                {
                    // 开始扫描，默认缓存4级文件节点
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    // 全盘扫描使用文件大小来估算进度会准确一些
                    _scanProgressEventer.Start(scanDirInfo);
                    InnerStartScan(scanDirInfo, _scannedRoot, 5, 1, cts);
                    _scannedRoot?.SetName(scanDirInfo.FullName);
                    stopwatch.Stop();
                    _logger.LogInformation("Scan elapsed:{elapsed}, path:{scanPath}", stopwatch.Elapsed, scanPath);
                    _scanProgressEventer.Complete(); // 内部会触发事件，不能lock

                    lock (_lock)
                    {
                        ScanState = ScanStates.Completed;
                    }

                    GC.Collect(); // 扫描过程中会产生大量废弃的对象，这里做一次GC可以让内存占用明显减少
                    RaiseScanStateChanged(ScanStates.Completed);
                }
                catch (Exception ex)
                {
                    lock (_lock)
                    {
                        ScanState = ScanStates.Idle;
                    }
                    RaiseScanStateChanged(ScanStates.Idle);
                    if (ex is OperationCanceledException)
                        _logger.LogWarning("Scan stopped: path={scanPath}", scanPath);
                    else
                        _logger.LogWarning(ex, "InnerStartScan failed: path={scanPath}", scanPath);
                }
            });
        }

        public void StopScan()
        {
            lock (_lock)
            {
                _scanCTS?.Cancel();
            }
        }

        public void CleanScan()
        {
            lock (_lock)
            {
                if (ScanState == ScanStates.Scanning) throw new ApplicationException("In scanning state.");
                if (ScanState == ScanStates.Cleaning) throw new ApplicationException("Already in cleaning state.");
                if (ScanState == ScanStates.Idle) return;

                ScanState = ScanStates.Idle;
                ScanPath = null;
                _scanProgressEventer.Reset();
                _scannedRoot?.CleanSubs();
                _scannedRoot = null;
                _scanCTS?.Dispose();
                _scanCTS = null;
            }
            RaiseScanStateChanged(ScanStates.Idle);
        }

        public ScannedFileSysInfo? GetTree(string? startPath, int deep, double ignoreSize)
        {
            lock (_lock)
            {
                if (string.IsNullOrEmpty(ScanPath) || _scannedRoot == null) throw new InvalidOperationException("Not scanned.");

                ScannedFileSysInfoCompressed? node;
                string? nodeFullPath = null;
                if (!string.IsNullOrEmpty(startPath))
                {
                    if (!startPath.StartsWith(ScanPath, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("startPath must be contains ScanPath", nameof(startPath));

                    string nodePath = startPath.Substring(ScanPath.Length).Trim('/', '\\');
                    if (!string.IsNullOrWhiteSpace(nodePath))
                        node = GetNodeByPath(_scannedRoot, nodePath);
                    else
                        node = _scannedRoot;

                    nodeFullPath = startPath;
                }
                else
                {
                    node = _scannedRoot;
                    nodeFullPath = ScanPath;
                }

                if (node == null)
                    return null;
                else
                    return node.Copy(nodeFullPath, node.GetName()!, deep, node.Size, ignoreSize);
            }
        }

        public void OpenFile(string path)
        {
        }

        public void OpenFolder(string path)
        {
        }

        public List<string> SearchFiles(string key)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region private methods
        private void _scanProgressEventer_ProgressChanged(object? sender, ScanProgressArgs e)
        {
            RaiseScanProgressChanged(e);
        }

        private ScannedFileSysInfoCompressed? GetNodeByPath(ScannedFileSysInfoCompressed root, string nodePath)
        {
            string[] parts = nodePath.Trim().TrimEnd('/', '\\').Split('/', '\\');
            if (parts.Length == 0) return null; // 路径为空

            ScannedFileSysInfoCompressed? parent = root;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parent == null) return null;

                if (parent.Subs != null)
                    parent = parent.Subs.FirstOrDefault(x => parts[i].Equals(x.GetName(), StringComparison.OrdinalIgnoreCase));
                else
                    return null;
            }

            return parent;
        }

        /// <summary>
        /// 扫描文件夹
        /// </summary>
        /// <param name="dirInfo">当前扫描的文件夹</param>
        /// <param name="resInfo">用于接收扫描结果，等同与返回值</param>
        /// <param name="fileFillDeep">大于1表示需要添加文件节点到Subs属性</param>
        /// <param name="processPart">这个文件夹占总扫描进度的百分比，值在0到1之间</param>
        /// <param name="cts">用于取消当前扫描</param>
        /// <returns></returns>
        private ScannedFileSysInfoCompressed InnerStartScan(DirectoryInfo dirInfo, ScannedFileSysInfoCompressed? resInfo, int fileFillDeep, double processPart, CancellationTokenSource cts)
        {
            if (resInfo == null)
                resInfo = new ScannedFileSysInfoCompressed();

            resInfo.IsDir = true;
            resInfo.IsFileFilled = fileFillDeep > 1;// > 1说明需要添加子文件节点到Subs属性
            resInfo.SetName(dirInfo.Name);

            var itemInfos = dirInfo.GetFileSystemInfos();
            int subFileFillDeep = fileFillDeep - 1;
            double subProcessPart = processPart / Math.Max(1, itemInfos.Length);
            double preProcess = _scanProgressEventer.CurrentProgress;
            foreach (var itemInfo in itemInfos)
            {
                if (itemInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    // itemInfo是一个文件夹

                    // 忽略连接
                    if (!string.IsNullOrEmpty(itemInfo.LinkTarget)) continue;

                    try
                    {
                        var subInfo = InnerStartScan((itemInfo as DirectoryInfo)!, null, subFileFillDeep, subProcessPart, cts);
                        if (subInfo.Size > 0)
                        {
                            // 为了节约内存，过滤大小为0的子文件夹
                            lock (_lock) // 使用lock防止GetTree调用时多线程同时操作Subs属性
                            {
                                resInfo.Size += subInfo.Size;
                                resInfo.AddSub(subInfo);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        // Access denied
                        // _logger.Warn($"Scan {itemInfo.Name} failed. {ex.Message}");
                    }
                }
                else
                {
                    // itemInfo是一个文件

                    // FileAttributes.Offline: The file is offline. The data of the file is not immediately available.
                    if (!itemInfo.Attributes.HasFlag(FileAttributes.Offline))
                    {
                        FileInfo fileInfo = (itemInfo as FileInfo)!;
                        resInfo.Size += fileInfo.Length;

                        if (resInfo.IsFileFilled && fileInfo.Length > 0)
                        {
                            // 为了节约内存，只取初始状态需要显示给用户看的文件节点，过滤大小为0的节点
                            var subFileInfo = new ScannedFileSysInfoCompressed { Size = fileInfo.Length };
                            subFileInfo.SetName(fileInfo.Name);
                            lock (_lock) // 使用lock防止GetTree调用时多线程同时操作Subs属性
                            {
                                resInfo.AddSub(subFileInfo);
                            }
                        }

                        if (_scanProgressEventer.Mode == ScanProgressModes.Size)
                            _scanProgressEventer.AddSize(fileInfo.Length);
                        else
                            _scanProgressEventer.Add(subProcessPart, fileInfo.Length);
                    }
                    else
                    {
                        if (_scanProgressEventer.Mode == ScanProgressModes.FileCount)
                            _scanProgressEventer.Add(subProcessPart, 0);
                    }
                }

                // 处理取消
                cts.Token.ThrowIfCancellationRequested();
            }// foreach (var itemInfo in itemInfos)

            // 补充没有统计到的链接子文件夹、空子文件夹、无权访问的子文件夹的进度
            if (_scanProgressEventer.Mode == ScanProgressModes.FileCount && _scanProgressEventer.CurrentProgress - preProcess < processPart)
                _scanProgressEventer.Add(processPart - _scanProgressEventer.CurrentProgress + preProcess, 0);

            return resInfo;
        }
        #endregion
    }
}
