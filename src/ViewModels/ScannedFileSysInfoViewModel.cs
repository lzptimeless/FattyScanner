using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FattyScanner.Core;
using FattyScanner.Core.Models;
using FattyScanner.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FattyScanner.ViewModels
{
    public class ScannedFileSysInfoViewModel : ObservableObject
    {
        #region fields
        private readonly ScannedFileSysInfoViewModel? _parent;
        private readonly ScannedFileSysInfo _scannedFileSysInfo;
        private readonly ILogger _logger;
        private readonly IScanModule _scanModule;
        #endregion

        public ScannedFileSysInfoViewModel(ScannedFileSysInfoViewModel? parent, ScannedFileSysInfo scannedFileSysInfo, IScanModule scanModule, double displayWidth)
        {
            _parent = parent;
            _scannedFileSysInfo = scannedFileSysInfo;
            _logger = AppLogger.CreateLogger(typeof(ScannedFileSysInfoViewModel));
            _scanModule = scanModule;
            DisplayWidth = displayWidth;

            var totalSize = Math.Max(1, _scannedFileSysInfo.Size);
            var subs = scannedFileSysInfo.Subs;
            if (subs != null && subs.Count > 0)
            {
                foreach (var sub in subs)
                {
                    Items.Add(new ScannedFileSysInfoViewModel(this, sub, scanModule, displayWidth: Math.Round(sub.Size / (double)totalSize * displayWidth, 1)));
                }
            }
        }

        #region properties
        public ScannedFileSysInfo InnerInfo
        {
            get { return _scannedFileSysInfo; }
        }

        public double DisplayWidth { get; private set; }

        public ObservableCollection<ScannedFileSysInfoViewModel> Items { get; private set; } = [];
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
                    _expandCommand = new RelayCommand(Expand);
                }

                return _expandCommand;
            }
        }
        private void Expand()
        {
            try
            {
                if (_scannedFileSysInfo.IsDir && Items.Count == 0)
                {
                    var fullPath = GetFullPath();
                    if (string.IsNullOrEmpty(fullPath))
                    {
                        _logger.LogWarning("Cannot get child items for an empty full path, current:{name}", _scannedFileSysInfo.Name);
                    }
                    else
                    {
                        var totalSize = Math.Max(1, _scannedFileSysInfo.Size);
                        var subs = _scanModule.GetTree(fullPath, deep: 2, ignoreSize: 0.01)?.Subs;
                        if (subs != null && subs.Count > 0)
                        {
                            foreach (var sub in subs)
                            {
                                Items.Add(new ScannedFileSysInfoViewModel(this, sub, _scanModule, displayWidth: Math.Round(sub.Size / (double)totalSize * DisplayWidth, 1)));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Getting child items failed, current: {name}", _scannedFileSysInfo.Name);
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
                var fullPath = GetFullPath();
                Clipboard.SetText(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Copying full path failed for {name}.", _scannedFileSysInfo.Name);
            }
        }
        #endregion
        #endregion

        #region private methods
        private string? GetFullPath()
        {
            var sb = new StringBuilder();
            var ancestor = _parent;
            while (ancestor != null)
            {
                var ancestorName = ancestor.InnerInfo.Name;
                if (!string.IsNullOrEmpty(ancestorName))
                {
                    if (sb.Length > 0)
                    {
                        sb.Insert(0, '\\');
                    }

                    sb.Insert(0, ancestorName);
                }

                ancestor = ancestor._parent;
            }

            return sb.ToString();
        }
        #endregion
    }
}
