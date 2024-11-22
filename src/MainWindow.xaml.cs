using FattyScanner.Converters;
using FattyScanner.ViewModels;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FattyScanner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            Loaded += MainWindow_Loaded;
            Unloaded += MainWindow_Unloaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _viewModel.IsActive = true;
        }

        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.IsActive = false;
        }

        private void OnScanResultTreeViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var displayWidthConverter = Resources["FileSysDisplayWidthConverter"] as FileSysDisplayWidthConverter;
            if (displayWidthConverter != null)
            {
                var totalWidth = ScanResultTreeView.ActualWidth;
                if (!double.IsInfinity(totalWidth) && !double.IsNaN(totalWidth) && totalWidth > 0)
                {
                    displayWidthConverter.TotalDisplayWidth = totalWidth;
                }
            }
        }
    }
}