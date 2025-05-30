using CefSharp;
using CefSharp.WinForms;
using SeniorWebHelper;
using System;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Media.Imaging;


namespace WebSurfingHelpersForSenior
{
    public partial class MainWindow : Window
    {
        private ChromiumWebBrowser winFormsBrowser;
        private double currentZoomLevel = 0.0;
        private TTSService ttsService = new TTSService();
        
        // 소리로 듣기, 중단을 위한 bool
        private bool isSpeaking = false;
        
        // 소리로 듣기, 중단을 위한 이미지
        private const string IconPlay = "https://cdn-icons-png.flaticon.com/512/1538/1538548.png";
        private const string IconStop = "https://cdn-icons-png.flaticon.com/512/5890/5890688.png";



        public MainWindow()
        {
            InitializeComponent();

            winFormsBrowser = new ChromiumWebBrowser("https://www.naver.com");
            browserHost.Child = winFormsBrowser;

            // 음성 완료 이벤트 등록
            ttsService.SpeakCompleted += TtsService_SpeakCompleted;

            this.Closed += MainWindow_Closed; // 창 닫힐 때 실행

        }

        // 백그라운드 실행 막음
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            winFormsBrowser.Dispose();           // CefSharp 해제
            Application.Current.Shutdown();      // 프로세스 종료
        }



        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel += 0.2;
            winFormsBrowser.SetZoomLevel(currentZoomLevel);
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel -= 0.2;
            winFormsBrowser.SetZoomLevel(currentZoomLevel);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (winFormsBrowser != null && winFormsBrowser.CanGoBack)
            {
                winFormsBrowser.Back();
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (winFormsBrowser != null && winFormsBrowser.CanGoForward)
            {
                winFormsBrowser.Forward();
            }
        }

        private async void SpeakButton_Click(object sender, RoutedEventArgs e)
        {
            if (isSpeaking)
            {
                ttsService.Stop();
                isSpeaking = false;
                SpeakButtonText.Text = "소리로 듣기";
                SpeakButtonImage.Source = new BitmapImage(new Uri(IconPlay));
            }
            else
            {
                var result = await winFormsBrowser.EvaluateScriptAsync("window.getSelection().toString();");
                if (result.Success && result.Result is string selectedText && !string.IsNullOrWhiteSpace(selectedText))
                {
                    ttsService.Speak(selectedText);
                    isSpeaking = true;
                    SpeakButtonText.Text = "중단";
                    SpeakButtonImage.Source = new BitmapImage(new Uri(IconStop));
                }
                else
                {
                    CustomMessageBox.Show("먼저 읽고 싶은 텍스트를 마우스로 선택해 주세요.\n(글자 위에 클릭 후 쭉 움직이기)", this);
                }
            }
        }



        private void TtsService_SpeakCompleted(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                isSpeaking = false;
                SpeakButtonText.Text = "소리로 듣기";
                SpeakButtonImage.Source = new BitmapImage(new Uri(IconPlay));
            });
        }



        // 설정 버튼 클릭 이벤트
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settings = new SettingsWindow(ttsService);
                settings.Owner = this;
                settings.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("설정창을 여는 중 오류 발생: " + ex.Message);
            }
        }






    }
}
