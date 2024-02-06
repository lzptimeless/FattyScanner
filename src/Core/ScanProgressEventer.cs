using FattyScanner.Core.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Core
{
    /// <summary>
    /// 辅助进度事件触发，控制触发频率
    /// </summary>
    internal class ScanProgressEventer
    {
        #region fields
        /// <summary>
        /// 相比上次触发事件所需的进度最小增量
        /// </summary>
        private readonly double _progressIncrement;
        /// <summary>
        /// 相比上次触发事件所需的最小事件间隔，单位毫秒
        /// </summary>
        private readonly long _minInterval;
        /// <summary>
        /// 当前进度值
        /// </summary>
        private double _currentProgress;
        /// <summary>
        /// 上次触发ProgressChanged的时间
        /// </summary>
        private long _preRaiseTick;
        /// <summary>
        /// 上次触发ProgressChanged的进度值
        /// </summary>
        private double _preRaiseProgress;
        #endregion

        /// <summary>
        /// 初始化<see cref="ScanProgressEventer"/>
        /// </summary>
        /// <param name="progressIncrement">相比上次触发事件所需的进度最小增量</param>
        /// <param name="minInterval">相比上次触发事件所需的最小事件间隔，单位毫秒</param>
        public ScanProgressEventer(double progressIncrement, long minInterval)
        {
            _progressIncrement = progressIncrement;
            _minInterval = minInterval;
        }

        #region properties
        /// <summary>
        /// 扫描进度估算模式
        /// </summary>
        public ScanProgressModes Mode { get; set; }

        public double TotalSize { get; set; }

        public long CurrentSize { get; set; }

        public double CurrentProgress
        {
            get { return _currentProgress; }
        }
        #endregion

        #region events
        public event EventHandler<ScanProgressArgs>? ProgressChanged;
        #endregion

        #region public methods
        public void Start(DirectoryInfo scanDir)
        {
            if (scanDir.Root.FullName == scanDir.FullName)
            {
                Mode = ScanProgressModes.Size;
                var driveInfo = new DriveInfo(scanDir.FullName);
                TotalSize = driveInfo.TotalSize - driveInfo.TotalFreeSpace;
            }
            else
                Mode = ScanProgressModes.FileCount;
        }

        /// <summary>
        /// 增加进度，内部有处理防止进度达到1，如果要达到1需要调用<see cref="Complete"/>
        /// </summary>
        /// <param name="progress"></param>
        public void Add(double progress, long size)
        {
            if (_currentProgress + progress >= 1) return;

            long sysTick = Environment.TickCount64;
            CurrentSize += size;
            _currentProgress += progress;
            if (sysTick - _preRaiseTick >= _minInterval && 
                _currentProgress - _preRaiseProgress >= _progressIncrement)
            {
                _preRaiseTick = sysTick;
                _preRaiseProgress = _currentProgress;

                ProgressChanged?.Invoke(this, new ScanProgressArgs(_currentProgress, CurrentSize));
            }
        }

        public void AddSize(long size)
        {
            if (CurrentSize + size > TotalSize) return;

            long sysTick = Environment.TickCount64;
            CurrentSize += size;
            _currentProgress = CurrentSize / TotalSize;
            if (sysTick - _preRaiseTick >= _minInterval &&
                _currentProgress - _preRaiseProgress >= _progressIncrement)
            {
                _preRaiseTick = sysTick;
                _preRaiseProgress = _currentProgress;

                ProgressChanged?.Invoke(this, new ScanProgressArgs(_currentProgress, CurrentSize));
            }
        }

        public void Complete()
        {
            _currentProgress = 1;
            _preRaiseTick = Environment.TickCount64;
            _preRaiseProgress = _currentProgress;
            ProgressChanged?.Invoke(this, new ScanProgressArgs(_currentProgress, CurrentSize));
        }

        public void Reset()
        {
            _currentProgress = 0;
            _preRaiseProgress = 0;
            _preRaiseTick = 0;

            TotalSize = 0;
            CurrentSize = 0;
        }
        #endregion
    }

    /// <summary>
    /// 扫描进度估算的模式
    /// </summary>
    internal enum ScanProgressModes
    {
        /// <summary>
        /// 通过文件数来估算进度，误差较大
        /// </summary>
        FileCount,
        /// <summary>
        /// 通过文件大小来估算进度，误差较小，但是只能用于全盘扫描
        /// </summary>
        Size
    }
}
