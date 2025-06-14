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
using System.Windows.Shapes;

namespace WebSurfingHelpersForSenior
{
    /// <summary>
    /// SetHomePageWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SetHomePageWindow : Window
    {
        public string SelectedUrl { get; private set; }

        public SetHomePageWindow()
        {
            InitializeComponent();
            Loaded += SetHomePageWindow_Loaded;
        }

        // 저장된 홈 주소를 입력창에 표시
        private void SetHomePageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string savedUrl = Properties.Settings.Default.HomeUrl;

            if (!string.IsNullOrWhiteSpace(savedUrl))
            {
                UrlTextBox.Text = savedUrl;
            }
            else
            {
                UrlTextBox.Text = "https://www.naver.com"; // 기본값
            }
        }

        private void Naver_Click(object sender, RoutedEventArgs e)
        {
            UrlTextBox.Text = "https://www.naver.com";
        }

        private void Google_Click(object sender, RoutedEventArgs e)
        {
            UrlTextBox.Text = "https://www.google.com";
        }

        private void Daum_Click(object sender, RoutedEventArgs e)
        {
            UrlTextBox.Text = "https://www.daum.net";
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            SelectedUrl = UrlTextBox.Text;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
