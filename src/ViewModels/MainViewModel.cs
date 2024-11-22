using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FattyScanner.Core;
using FattyScanner.Core.Models;
using FattyScanner.Logger;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.ViewModels
{
    internal class MainViewModel : ObservableRecipient
    {
        #region fields
        private readonly ILogger _logger;
        private readonly IScanModule _scanModule;
        #endregion

        public MainViewModel()
        {
            _logger = AppLogger.CreateLogger(typeof(MainViewModel));
            _scanModule = new ScanModule(AppLogger.Factory);
        }

        #region properties
        private string? _scanPath;
        public string? ScanPath
        {
            get { return _scanPath; }
            set { SetProperty(ref _scanPath, value); }
        }

        private double _scanProgressValue;
        public double ScanProgressValue
        {
            get { return _scanProgressValue; }
            set { SetProperty(ref _scanProgressValue, value); }
        }

        private long _scannedSize;
        public long ScannedSize
        {
            get { return _scannedSize; }
            set { SetProperty(ref _scannedSize, value); }
        }

        public ScanStates _scanState;
        public ScanStates ScanState
        {
            get { return _scanState; }
            set { SetProperty(ref _scanState, value); }
        }

        public FileSysNodeViewModel? _root;
        public ObservableCollection<FileSysNodeViewModel>? Subs
        {
            get { return _root?.Subs; }
        }
        #endregion

        #region commands
        #region BrowseScanPathCommand
        private RelayCommand? _browseScanPathCommand;
        public RelayCommand BrowseScanPathCommand
        {
            get
            {
                if (_browseScanPathCommand == null)
                {
                    _browseScanPathCommand = new RelayCommand(BrowseScanPath);
                }
                return _browseScanPathCommand;
            }
        }
        private void BrowseScanPath()
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                var folderPath = dialog.FolderName;
                ScanPath = folderPath;
            }
        }
        #endregion

        #region ScanCommand
        private RelayCommand? _scanCommand;
        public RelayCommand ScanCommand
        {
            get
            {
                if (_scanCommand == null)
                {
                    _scanCommand = new RelayCommand(Scan);
                }
                return _scanCommand;
            }
        }
        private void Scan()
        {
            var scanPath = ScanPath?.Trim();
            if (string.IsNullOrEmpty(scanPath))
            {
                _logger.LogWarning("Cannot scan using an empty scan path.");
                return;
            }

            try
            {
                _scanModule.StartScan(scanPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Scanning failed for path: {scanPath}", scanPath);
            }
        }
        #endregion

        #region StopCommand
        private RelayCommand? _stopCommand;
        public RelayCommand StopCommand
        {
            get
            {
                if (_stopCommand == null)
                {
                    _stopCommand = new RelayCommand(Stop);
                }
                return _stopCommand;
            }
        }
        private void Stop()
        {
            try
            {
                if (_scanModule.ScanState == ScanStates.Scanning)
                {
                    _scanModule.StopScan();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Stoping the scanning failed for path: {ScanPath}", ScanPath);
            }
        }
        #endregion
        #endregion

        #region private method
        protected override void OnActivated()
        {
            base.OnActivated();

            ScanState = _scanModule.ScanState;
            _scanModule.ScanStateChanged += OnScanStateChanged;
            _scanModule.ScanProgressChanged += OnScanProgressChanged;
        }

        protected override void OnDeactivated()
        {
            _scanModule.ScanStateChanged -= OnScanStateChanged;
            _scanModule.ScanProgressChanged -= OnScanProgressChanged;

            base.OnDeactivated();
        }

        private void OnScanStateChanged(object? sender, Core.Events.ScanStateChangedArgs e)
        {
            App.Current.Dispatcher.BeginInvoke(() =>
            {
                ScanState = e.State;
                if (e.State == ScanStates.Completed || e.State == ScanStates.Stopped)
                {
                    var scanResult = _scanModule.ScanResult;
                    if (scanResult != null)
                    {
                        _root = new FileSysNodeViewModel(scanResult, scanResult.Size, _scanModule);
                        _root.Expand();
                        OnPropertyChanged(nameof(Subs));
                    }
                }
            });
        }

        private void OnScanProgressChanged(object? sender, Core.Events.ScanProgressArgs e)
        {
            App.Current.Dispatcher.BeginInvoke(() =>
            {
                ScanProgressValue = e.ProgressValue * 100;
                ScannedSize = e.ScannedSize;
            });
        }
        #endregion
    }
}
