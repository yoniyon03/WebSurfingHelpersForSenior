using CefSharp;
using CefSharp.WinForms;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;



namespace WebSurfingHelpersForSenior
{
    // 팝업 차단
    public class CustomLifeSpanHandler : ILifeSpanHandler
    {
        public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser) => false;

        public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser) { }

        public bool OnBeforePopup(IWebBrowser chromiumWebBrowser,
                                  IBrowser browser,
                                  IFrame frame,
                                  string targetUrl,
                                  string targetFrameName,
                                  WindowOpenDisposition targetDisposition,
                                  bool userGesture,
                                  IPopupFeatures popupFeatures,
                                  IWindowInfo windowInfo,
                                  IBrowserSettings browserSettings,
                                  ref bool noJavascriptAccess,
                                  out IWebBrowser newBrowser)
        {
            newBrowser = null;

            // 새 창 대신 현재 탭에서 열기
            chromiumWebBrowser.Load(targetUrl);
            return true; // 팝업 차단
        }
    }
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


        // AI
        private GeminiService geminiService = new GeminiService();
        private string lastSuggestedQuestion = "";


        // 고대비
        private bool isHighContrast = false;



        public MainWindow()
        {
            InitializeComponent();

            winFormsBrowser = new ChromiumWebBrowser("https://www.naver.com");
            winFormsBrowser.LifeSpanHandler = new CustomLifeSpanHandler();
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


        // ai
        //private async void AIButton_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        var url = winFormsBrowser.Address;

        //        if (string.IsNullOrWhiteSpace(url))
        //        {
        //            CustomMessageBox.Show("웹페이지를 불러온 후 다시 시도해 주세요.", this);
        //            return;
        //        }

        //        string prompt = $"다음은 사용자가 보고 있는 웹페이지입니다: {url}\n" +
        //                        "이 사용자에게 도움이 될만한 질문이나 추천을 시니어가 이해하기 쉬운 말로 한글로 1~2개 제안해줘";

        //        string answer = await geminiService.GetRecommendation(prompt);

        //        if (!string.IsNullOrWhiteSpace(answer))
        //        {
        //            lastSuggestedQuestion = answer.Trim();

        //            // 말풍선 표시
        //            TextBlock balloon = new TextBlock
        //            {
        //                Text = $"💬 {lastSuggestedQuestion}",
        //                Background = System.Windows.Media.Brushes.LightYellow,
        //                Padding = new Thickness(10),
        //                FontSize = 14,
        //                FontWeight = FontWeights.Bold,
        //                Margin = new Thickness(20),
        //            };
        //            var layer = new Window
        //            {
        //                Content = balloon,
        //                Width = 300,
        //                Height = 80,
        //                WindowStyle = WindowStyle.None,
        //                AllowsTransparency = true,
        //                Background = null,
        //                Topmost = true,
        //                Left = this.Left + 150,
        //                Top = this.Top + 100,
        //                ShowInTaskbar = false
        //            };
        //            layer.Show();

        //            // 말풍선 닫은 후
        //            await Task.Delay(4000);
        //            layer.Close();

        //            // 👉 사이드 패널 열기
        //            aiSidebar.Visibility = Visibility.Visible;
        //            aiSidebarText.Text = lastSuggestedQuestion;
        //            aiAnswerText.Text = ""; // 답변 초기화

        //        }
        //        else
        //        {
        //            CustomMessageBox.Show("AI가 적절한 제안을 찾지 못했습니다.", this);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        CustomMessageBox.Show("AI 처리 중 오류 발생: " + ex.Message, this);
        //    }
        //}

        //private async void AskFollowUp_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        string question = aiSidebarText.Text;
        //        if (string.IsNullOrWhiteSpace(question)) return;

        //        aiAnswerText.Text = "AI가 응답 중입니다...";

        //        string answer = await geminiService.GetRecommendation("다음 질문에 대해 한국어로 친절하게 대답해줘: " + question);

        //        aiAnswerText.Text = answer;
        //    }
        //    catch (Exception ex)
        //    {
        //        aiAnswerText.Text = "AI 응답 중 오류 발생: " + ex.Message;
        //    }
        //}

        private async void AIButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var url = winFormsBrowser.Address;
                if (string.IsNullOrWhiteSpace(url))
                {
                    CustomMessageBox.Show("웹페이지를 불러온 후 다시 시도해 주세요.", this);
                    return;
                }

                aiSidebarText.Text = "AI가 분석 중입니다...";
                aiSidebar.Visibility = Visibility.Visible;
                aiAnswerText.Text = "";

                string answer = await geminiService.GetRecommendation(url);

                if (!string.IsNullOrWhiteSpace(answer))
                {
                    lastSuggestedQuestion = answer.Trim();
                    aiSidebarText.Text = lastSuggestedQuestion;
                }
                else
                {
                    aiSidebarText.Text = "AI가 적절한 제안을 찾지 못했습니다.";
                }
            }
            catch (Exception ex)
            {
                aiSidebarText.Text = "AI 처리 중 오류 발생: " + ex.Message;
            }
        }

        private async void AskFollowUp_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string question = aiSidebarText.Text;
                if (string.IsNullOrWhiteSpace(question)) return;

                aiAnswerText.Text = "AI가 응답 중입니다...";
                string answer = await geminiService.GetRecommendation("다음 질문에 대해 한국어로 친절하게 대답해줘: " + question);
                aiAnswerText.Text = answer;
            }
            catch (Exception ex)
            {
                aiAnswerText.Text = "AI 응답 중 오류 발생: " + ex.Message;
            }
        }


        // 고대비
        private void HighContrast_Click(object sender, RoutedEventArgs e)
        {
            // 토글 상태 전환
            isHighContrast = !isHighContrast;

            // 색상 정의
            var bg = isHighContrast ? Brushes.Black : Brushes.White;
            var fg = isHighContrast ? Brushes.Yellow : Brushes.Black;
            var border = isHighContrast ? Brushes.Orange : Brushes.Gray;
            var weight = isHighContrast ? FontWeights.Bold : FontWeights.Normal;

            // 윈도우 전체 배경
            this.Background = bg;

            // 주요 WPF 컨트롤 스타일 변경
            foreach (var ctrl in LogicalTreeHelper.GetChildren(this))
            {
                if (ctrl is Control c)
                {
                    c.Background = bg;
                    c.Foreground = fg;
                    c.FontWeight = weight;

                    if (c is Button || c is ToggleButton)
                    {
                        c.BorderBrush = border;
                    }
                }
            }


            // CefSharp 고대비 적용 (이미지, 영상, iframe 제외)
            var chrome = browserHost.Child as CefSharp.WinForms.ChromiumWebBrowser;
            if (chrome != null && chrome.IsBrowserInitialized)
            {
                string script = isHighContrast
                    ? @"
        Array.from(document.querySelectorAll('*')).forEach(el => {
            if (!(el instanceof HTMLImageElement || el.tagName === 'VIDEO' || el.tagName === 'IFRAME')) {
                el.style.setProperty('background-color', 'black', 'important');
                el.style.setProperty('color', 'yellow', 'important');
                el.style.setProperty('border-color', 'orange', 'important');
                if (el.tagName === 'A') {
                    el.style.setProperty('color', '#00ffff', 'important');
                }
            }
        });
      "
                    : @"
        Array.from(document.querySelectorAll('*')).forEach(el => {
            el.style.removeProperty('background-color');
            el.style.removeProperty('color');
            el.style.removeProperty('border-color');
        });
      ";

                chrome.ExecuteScriptAsync(script);
            }
        }


    }
}
