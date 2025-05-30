using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WebSurfingHelpersForSenior
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private double currentZoomLevel = 0.0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel += 0.2;
            webView.ZoomLevel = currentZoomLevel;
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel -= 0.2;
            webView.ZoomLevel = currentZoomLevel;
        }
    }
}
