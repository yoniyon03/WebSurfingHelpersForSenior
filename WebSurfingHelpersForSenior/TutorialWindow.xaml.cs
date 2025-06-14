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
            // ✅ 튜토리얼 단계 창 열기
            var stepsWindow = new TutorialStepsWindow();
            stepsWindow.Show();

            // ✅ 현재 인트로 창 닫기
            this.Close();
        }


    }
}
