using CommunityToolkit.Mvvm.ComponentModel;
using FattyScanner.Core.Models;
using FattyScanner.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FattyScanner.Logger;
using CommunityToolkit.Mvvm.Input;
using System.Windows;
using System.Collections.ObjectModel;

namespace FattyScanner.ViewModels
{
    public class FileSysNodeViewModel : ObservableObject
    {
        #region fields
        private readonly FileSysNode _fileSysNode;
        private readonly ILogger _logger;
        private readonly ulong _totalSize;
        private readonly IScanModule _scanModule;
        #endregion

        public FileSysNodeViewModel(FileSysNode fileSysNode, ulong totalSize, IScanModule scanModule)
        {
            if (totalSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalSize), "totalSize must > 0");

            _fileSysNode = fileSysNode;
            _logger = AppLogger.CreateLogger(typeof(FileSysNodeViewModel));
            _totalSize = totalSize;
            _scanModule = scanModule;
            DisplayPercentage = ((double)fileSysNode.Size) / totalSize;
        }

        #region properties
        public string? Name => _fileSysNode.Name;
        public ulong Size => _fileSysNode.Size;
        public bool IsDir => _fileSysNode.IsDir;
        public double DisplayPercentage { get; private set; }
        public ObservableCollection<FileSysNodeViewModel> Subs { get; private set; } = new ObservableCollection<FileSysNodeViewModel>();
        #endregion

        #region commands
        #region ExpandCommand
        private RelayCommand? _expandCommand;
        public RelayCommand ExpandCommand
        {
            get
            {
                if (_expandCommand == null)
                {
                    _expandCommand = new RelayCommand(Expand, CanExpand);
                }

                return _expandCommand;
            }
        }

        private bool CanExpand()
        {
            if (_fileSysNode.IsDir && Subs.Count == 0)
            {
                return true;
            }
            return false;
        }

        public void Expand()
        {
            // 已经展开过了
            if (Subs.Count > 0) return;
            // 只有文件夹才能展开
            if (!_fileSysNode.IsDir) return;

            try
            {
                _scanModule.Expand(_fileSysNode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Expand failed: {_fileSysNode.GetFullPath()}");
            }

            if (_fileSysNode.Subs != null)
            {
                foreach (var subFileSysNode in _fileSysNode.Subs)
                {
                    var subPercentage = subFileSysNode.Size / (double)_fileSysNode.Size;
                    // 为了优化UI元素数量，子文件或子文件夹的大小占比大于0.07才显示
                    if (subPercentage >= 0.07)
                    {
                        Subs.Add(new FileSysNodeViewModel(subFileSysNode, _totalSize, _scanModule));
                    }
                }
                _expandCommand?.NotifyCanExecuteChanged();
            }
        }
        #endregion

        #region CopyFullPathCommand
        private RelayCommand? _copyFullPathCommand;
        public RelayCommand CopyFullPathCommand
        {
            get
            {
                if (_copyFullPathCommand == null)
                {
                    _copyFullPathCommand = new RelayCommand(CopyFullPath);
                }
                return _copyFullPathCommand;
            }
        }
        private void CopyFullPath()
        {
            try
            {
                var fullPath = _fileSysNode.GetFullPath();
                Clipboard.SetText(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Copying full path failed for {name}.", _fileSysNode.Name);
            }
        }
        #endregion

        #region OpenFolderCommand
        private RelayCommand? _openFolderCommand;
        public RelayCommand OpenFolderCommand
        {
            get
            {
                if (_openFolderCommand == null)
                {
                    _openFolderCommand = new RelayCommand(OpenFolder, CanOpenFolder);
                }
                return _openFolderCommand;
            }
        }
        private void OpenFolder()
        {
            var path = _fileSysNode.GetFullPath();
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Open folder failed: {path}");
            }
        }
        private bool CanOpenFolder()
        {
            return _fileSysNode.IsDir;
        }
        #endregion
        #endregion
    }
}
