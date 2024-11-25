using FattyScanner.Core.Events;
using FattyScanner.Core.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FattyScanner.Core
{
    /// <summary>
    /// 扫描功能模块
    /// </summary>
    public class ScanModule : IScanModule
    {
        #region fields
        private readonly object _lock = new object();
        private readonly ILogger _logger;
        /// <summary>
        /// 文件扫描进度预测器
        /// </summary>
        private readonly ScanProgressPredictor _progressPredictor;
        /// <summary>
        /// 用于停止扫描的Token
        /// </summary>
        private CancellationTokenSource? _scanCTS;
        #endregion

        public ScanModule(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(typeof(ScanModule));
            _progressPredictor = new ScanProgressPredictor();
            _progressPredictor.ScanProgressChanged += OnScanProgressChanged;
        }

        #region properties
        public ScanStates ScanState { get; private set; }
        public string? ScanPath { get; private set; }
        public FileSysNode? ScanResult { get; private set; }
        #endregion

        #region events
        public event EventHandler<ScanProgressArgs>? ScanProgressChanged;
        public event EventHandler<ScanStateChangedArgs>? ScanStateChanged;
        #endregion

        #region public methods
        public async void StartScan(string path)
        {
            lock (_lock)
            {
                if (ScanState == ScanStates.Scanning || ScanState == ScanStates.Cleaning)
                {
                    throw new InvalidOperationException($"State error, state: {ScanState}");
                }

                ScanState = ScanStates.Scanning;
            }
            _logger.LogInformation($"Scan start, path: {path}");
            ScanStateChanged?.Invoke(this, new ScanStateChangedArgs(ScanStates.Scanning));

            try
            {
                // 重置状态
                if (_scanCTS != null)
                {
                    _scanCTS.Cancel();
                    _scanCTS.Dispose();
                }
                _scanCTS = new CancellationTokenSource();
                _progressPredictor.Reset();
                if (ScanResult != null)
                {
                    // 清理旧数据，有助于内存垃圾回收
                    await Task.Run(() => ScanResult.CleanSub());
                }
                ScanResult = new FileSysNode();
                ScanResult.ScanPath = path;

                // 开始扫描
                var dirInfo = new DirectoryInfo(path);
                if (!dirInfo.Exists)
                {
                    throw new DirectoryNotFoundException($"Not found {path}");
                }
                ScanPath = path;
                var progressPredictorSub = _progressPredictor.CreateRoot();
                var ct = _scanCTS.Token;
                Stopwatch stopwatch = Stopwatch.StartNew();
                await Task.Run(() => ScanInner(dirInfo, ScanResult, progressPredictorSub, ct));
                _progressPredictor.Complete(); // 触发100%事件
                GC.Collect(); // 扫描过程中会产生大量废弃的对象，这里做一次GC可以让内存占用明显减少
                stopwatch.Stop();
                var resultState = ct.IsCancellationRequested ? ScanStates.Stopped : ScanStates.Completed;
                _logger.LogInformation($"Scan {resultState}, size: {ByteConverter.Format(ScanResult.Size)}, elapsed: {stopwatch.Elapsed}");
                lock (_lock)
                {
                    ScanState = resultState;
                }
                ScanStateChanged?.Invoke(this, new ScanStateChangedArgs(resultState));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Scan failed.");
                lock (_lock)
                {
                    ScanState = ScanStates.ScanFailed;
                }
                ScanStateChanged?.Invoke(this, new ScanStateChangedArgs(ScanStates.ScanFailed));
            }
        }

        public void StopScan()
        {
            lock (_lock)
            {
                _scanCTS?.Cancel();
            }
        }

        public void Expand(FileSysNode node)
        {
            // 只有文件夹才能展开
            if (!node.IsDir) return;

            var path = node.GetFullPath();
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                throw new ApplicationException($"Expand directory failed, path not exists: {path}");
            }

            var fileInfos = dirInfo.GetFiles();
            foreach (var fileInfo in fileInfos)
            {
                // 离线文件不在本地磁盘，不需要统计，否则会出发系统自动下载，会很慢
                if (fileInfo.Attributes.HasFlag(FileAttributes.Offline)) continue;

                var fileLen = fileInfo.Length;
                var subNode = new FileSysNode();
                subNode.Name = fileInfo.Name;
                subNode.IsDir = false;
                subNode.Size = (ulong)fileLen;
                subNode.Parent = node;
                node.AddSub(subNode);
            }
        }
        #endregion

        #region private methods
        private void OnScanProgressChanged(object? sender, ScanProgressArgs e)
        {
            ScanProgressChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 递归扫描文件夹
        /// </summary>
        /// <param name="parentDirInfo">要扫描的目标文件夹信息</param>
        /// <param name="parentNode">用于接收扫描结果的对象</param>
        /// <param name="progressPredictorSub">用于统计当前文件夹扫描进度的工具</param>
        /// <param name="cts">用于停止扫描的Token</param>
        private void ScanInner(DirectoryInfo parentDirInfo, FileSysNode parentNode, ScanProgressPredictorSub progressPredictorSub, CancellationToken ct)
        {
            parentNode.Name = parentDirInfo.Name;
            parentNode.IsDir = true;

            var fileSysInfos = parentDirInfo.GetFileSystemInfos();
            progressPredictorSub.SetTotalFileSysCount(fileSysInfos.LongLength);

            foreach (var fileSysInfo in fileSysInfos)
            {
                try
                {
                    if (ct.IsCancellationRequested) return;

                    if (fileSysInfo is DirectoryInfo dirInfo)
                    {
                        // 忽略链接，因为链接可能会导致循环引用，也不是真实的文件夹
                        if (!string.IsNullOrEmpty(dirInfo.LinkTarget)) continue;

                        var subNode = new FileSysNode();

                        try
                        {
                            ScanInner(dirInfo, subNode, progressPredictorSub.CreateSub(), ct);
                        }
                        catch (Exception)
                        {
                            // Access denied
                            // _logger.Warn($"Scan {itemInfo.Name} failed. {ex.Message}");
                            continue;
                        }

                        // 如果子文件夹为空就没有统计的必要，同时也为了节约内存空间
                        if (subNode.Size > 0)
                        {
                            subNode.Parent = parentNode;
                            parentNode.Size += subNode.Size;
                            parentNode.AddSub(subNode);
                        }
                    }
                    else if (fileSysInfo is FileInfo fileInfo)
                    {
                        // 离线文件不在本地磁盘，不需要统计，否则会出发系统自动下载，会很慢
                        if (fileInfo.Attributes.HasFlag(FileAttributes.Offline)) continue;

                        var fileLen = fileInfo.Length;
                        parentNode.Size += (ulong)fileLen;
                        progressPredictorSub.AddFileSize((ulong)fileLen);

                        // 文件节点太多了太占内存，扫描时不保存文件节点，在用户需要查看时再从系统读取
                        // var subNode = new FileSysNode();
                        // subNode.Name = fileInfo.Name;
                        // subNode.IsDir = false;
                        // subNode.Size = (ulong)fileLen;
                        // subNode.Parent = parentNode;
                        // parentNode.AddSub(subNode);
                    }
                }
                finally
                {
                    // 当前文件夹扫描进度加1
                    progressPredictorSub.AddOne();
                }
            } // foreach (var fileSysInfo in fileSysInfos)
        }
        #endregion
    }
}
