using FattyScanner.Core.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.Core
{
    /// <summary>
    /// 文件扫描进度预测器
    /// </summary>
    public class ScanProgressPredictor
    {
        #region fields
        private const long MinProgressChangeSize = 1024 * 1024;
        private const long MinProgressChangeMilliseconds = 300;
        private const double MinProgressChangeValue = 0.001;

        private ScanProgressPredictorSub? _root;
        private long _totalSize;
        private long _preEventTotalSize;
        private long _preEventTime;
        private double _preEventProgressValue;
        private bool _isCompleted;
        #endregion

        public event EventHandler<ScanProgressArgs>? ScanProgressChanged;

        #region public methods
        /// <summary>
        /// 创建扫描路径所代表的根目录（首个目录）的进度统计工具
        /// </summary>
        /// <returns></returns>
        public ScanProgressPredictorSub CreateRoot()
        {
            _root = new ScanProgressPredictorSub(1, OnProgressChanged, OnSizeAdded);
            return _root;
        }

        /// <summary>
        /// 获取当前总的扫描进度
        /// </summary>
        /// <returns></returns>
        public ScanProgressArgs GetCurrentProgress()
        {
            double currentProgressValue = 0;
            if (_isCompleted)
            {
                currentProgressValue = 1;
            }
            else
            {
                currentProgressValue = _root?.GetCurrentProgressValue() ?? 0;
            }

            return new ScanProgressArgs(currentProgressValue, _totalSize);
        }

        /// <summary>
        /// 触发进度达到100%的事件
        /// </summary>
        public void Complete()
        {
            _isCompleted = true;
            ScanProgressChanged?.Invoke(this, new ScanProgressArgs(1, _totalSize));
        }

        /// <summary>
        /// 重置到最初状态
        /// </summary>
        public void Reset()
        {
            _root = null;
            _totalSize = 0;
            _preEventTotalSize = 0;
            _preEventTime = 0;
            _preEventProgressValue = 0;
            _isCompleted = false;
        }
        #endregion

        #region private methods
        private void OnProgressChanged()
        {
            if (_root == null) return;
            if (_isCompleted) return;

            var currentTick = Environment.TickCount64;
            if (currentTick - _preEventTime < MinProgressChangeMilliseconds) return;

            var currentProgressValue = _root.GetCurrentProgressValue();
            if (currentProgressValue - _preEventProgressValue < MinProgressChangeValue &&
                _totalSize - _preEventTotalSize < MinProgressChangeSize) return;

            _preEventTotalSize = _totalSize;
            _preEventTime = currentTick;
            _preEventProgressValue = currentProgressValue;

            ScanProgressChanged?.Invoke(this, new ScanProgressArgs(currentProgressValue, _totalSize));
        }

        private void OnSizeAdded(long sizeIncrement)
        {
            _totalSize += sizeIncrement;
        }
        #endregion
    }

    /// <summary>
    /// 用于统计当前文件夹扫描进度的工具
    /// </summary>
    public class ScanProgressPredictorSub
    {
        #region fields
        /// <summary>
        /// 当前文件夹在总扫描进度中所代表的数值
        /// </summary>
        private readonly double _totalProgressValue;
        /// <summary>
        /// 扫描进度改变时的通知函数
        /// </summary>
        private readonly Action _progressChangedAction;
        /// <summary>
        /// 总扫描大小增加的通知函数
        /// </summary>
        private readonly Action<long> _addSizeAction;
        /// <summary>
        /// 当前文件夹下文件与子文件夹的个数
        /// </summary>
        private long _totalFileSysCount;
        /// <summary>
        /// 当前文件夹下已经扫描完毕的文件与子文件夹的个数
        /// </summary>
        private long _scannedFileSysCount;
        /// <summary>
        /// 子文件夹的进度统计工具
        /// </summary>
        private ScanProgressPredictorSub? _sub;
        #endregion

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="totalProgressValue">当前文件夹在总扫描进度中所代表的数值</param>
        /// <param name="progressChangedAction">扫描进度改变时的通知函数</param>
        /// <param name="addSizeAction">总扫描大小增加的通知函数</param>
        public ScanProgressPredictorSub(double totalProgressValue, Action progressChangedAction, Action<long> addSizeAction)
        {
            _totalProgressValue = totalProgressValue;
            _progressChangedAction = progressChangedAction;
            _addSizeAction = addSizeAction;
        }

        #region public methods
        /// <summary>
        /// 创建用于子文件夹的<see cref="ScanProgressPredictorSub"/>对象
        /// </summary>
        /// <returns></returns>
        public ScanProgressPredictorSub CreateSub()
        {
            if (_totalFileSysCount == 0)
                throw new InvalidOperationException("_totalFileSysCount is zero.");

            _sub = new ScanProgressPredictorSub(_totalProgressValue / _totalFileSysCount, _progressChangedAction, _addSizeAction);
            return _sub;
        }

        /// <summary>
        /// 设置当前文件夹下文件与子文件夹的个数
        /// </summary>
        /// <param name="total"></param>
        public void SetTotalFileSysCount(long total)
        {
            _totalFileSysCount = total;
        }

        /// <summary>
        /// 当前文件夹扫描进度加1，即某个子文件或子文件夹扫描完毕
        /// </summary>
        public void AddOne()
        {
            ++_scannedFileSysCount;
            // 这个子文件夹已经扫描完毕，清理掉这个子文件夹的进度统计工具
            _sub = null;
            // 通知外部扫描进度发生了变化
            _progressChangedAction();
        }

        /// <summary>
        /// 增加扫描结果的总大小
        /// </summary>
        /// <param name="sizeIncrement">增加的文件大小</param>
        public void AddFileSize(long sizeIncrement)
        {
            _addSizeAction(sizeIncrement);
        }

        /// <summary>
        /// 获取当前文件夹拥有的扫描进度值（注意不是总的当前扫描进度）
        /// </summary>
        /// <returns></returns>
        public double GetCurrentProgressValue()
        {
            double currentProgressValue = _totalProgressValue * (_scannedFileSysCount / (double)_totalFileSysCount);
            if (_sub != null)
            {
                currentProgressValue += _sub.GetCurrentProgressValue();
            }
            return currentProgressValue;
        }
        #endregion
    }
}
