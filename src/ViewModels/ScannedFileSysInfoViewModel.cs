using CommunityToolkit.Mvvm.ComponentModel;
using FattyScanner.Core.Models;
using FattyScanner.Logger;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FattyScanner.ViewModels
{
    public class ScannedFileSysInfoViewModel : ObservableObject
    {
        #region fields
        private readonly ScannedFileSysInfo _scannedFileSysInfo;
        private readonly ILogger _logger;
        #endregion

        public ScannedFileSysInfoViewModel(ScannedFileSysInfo scannedFileSysInfo)
        {
            _scannedFileSysInfo = scannedFileSysInfo;
            _logger = AppLogger.CreateLogger(typeof(ScannedFileSysInfoViewModel));
        }

        #region properties
        public ScannedFileSysInfo InnerInfo
        {
            get { return _scannedFileSysInfo; }
        }


        #endregion
    }
}
