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
    /// TutorialWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TutorialWindow : Window
    {
        public TutorialWindow()
        {
            InitializeComponent();
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SkipTutorial = DontShowAgainCheck.IsChecked == true;
            Properties.Settings.Default.Save();

            // 슬라이드 창 열기
            var stepsWindow = new TutorialStepsWindow();
            stepsWindow.Owner = this;
            stepsWindow.ShowDialog();

            this.Close();
        }

    }
}
