using CefSharp;
using CefSharp.WinForms;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using WinForms = System.Windows.Forms;

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
        private double currentZoomLevel = -0.5;
        private TTSService ttsService = new TTSService();

        // 소리로 듣기, 중단을 위한 bool
        private bool isSpeaking = false;

        // 소리로 듣기, 중단을 위한 이미지
        private const string IconPlay = "https://cdn-icons-png.flaticon.com/512/1538/1538548.png";
        private const string IconStop = "https://cdn-icons-png.flaticon.com/512/5890/5890688.png";

        // 고대비
        private bool isHighContrast = false;

        // 이미지 고대비 필터
        private bool isMediaFilterOff = false; // 기본값 - 필터 on

        // 글씨 크기
        private double currentFontSize = 1.0;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;

            winFormsBrowser = new ChromiumWebBrowser("https://www.naver.com");
            winFormsBrowser.LifeSpanHandler = new CustomLifeSpanHandler(); // 팝업차단

            winFormsBrowser.FrameLoadEnd += (sender, args) =>
            {
                if (args.Frame.IsMain)
                {
                    Dispatcher.Invoke(() =>
                    {
                        // 고대비 먼저
                        if (isHighContrast)
                        {
                            string markImagesNoContrast = @"
Array.from(document.querySelectorAll('img')).forEach(img => {
    const src = img.getAttribute('src') || '';
    if (src.includes('stat') || src.includes('pitch') || src.includes('profile') || src.includes('face')) {
        img.classList.add('no-contrast');
    }
});
";
                            args.Frame.ExecuteJavaScriptAsync(markImagesNoContrast);

                            string contrastScript = GetHighContrastScript(isHighContrast, !isMediaFilterOff);
                            args.Frame.ExecuteJavaScriptAsync(contrastScript);
                        }

                        // 폰트 커스터마이징한 경우에만 적용
                        if (Properties.Settings.Default.IsFontCustomized)
                        {
                            string fontName = Properties.Settings.Default.SelectedFontName;
                            float fontSize = Properties.Settings.Default.SelectedFontSize;
                            bool isBold = Properties.Settings.Default.IsFontBold;
                            bool isItalic = Properties.Settings.Default.IsFontItalic;

                            var style = System.Drawing.FontStyle.Regular;
                            if (isBold) style |= System.Drawing.FontStyle.Bold;
                            if (isItalic) style |= System.Drawing.FontStyle.Italic;

                            var savedFont = new System.Drawing.Font(fontName, fontSize, style);
                            ApplyBrowserFont(savedFont);
                        }
                    });
                }
            };

            browserHost.Child = winFormsBrowser;

            // 음성 완료 이벤트 등록
            ttsService.SpeakCompleted += TtsService_SpeakCompleted;

            this.Closed += MainWindow_Closed;

        }

        // 튜토리얼 창 보여주기
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 앱 시작 시 사용자 지정 폰트 적용 안 되게 초기화 (필요 시 지우기)
            Properties.Settings.Default.IsFontCustomized = false;
            Properties.Settings.Default.SkipTutorial = false;
            Properties.Settings.Default.Save();

            if (!Properties.Settings.Default.SkipTutorial)
            {
                var tutorial = new TutorialWindow();
                tutorial.Owner = this;
                tutorial.ShowDialog();
            }
        }

        // 폰트 변경
        public void ApplyBrowserFont(System.Drawing.Font fontName)
        {
            if (winFormsBrowser.IsBrowserInitialized)
            {
                // 스타일 분기
                string fontWeight = fontName.Bold ? "bold" : "normal";
                string fontStyle = fontName.Italic ? "italic" : "normal";
                string fontSize = $"{fontName.SizeInPoints}px";

                string js = $@"
            (function() {{
                const existing = document.getElementById('custom-font-style');
                if (existing) existing.remove();

                const style = document.createElement('style');
                style.id = 'custom-font-style';
                style.innerHTML = 
                    * {{
                        font-family: '{fontName.Name}' !important;
                        font-weight: {fontWeight} !important;
                        font-style: {fontStyle} !important;
                        font-size: {fontSize} !important;
                    }}
                ;
                document.head.appendChild(style);
            }})();
        ";
                winFormsBrowser.ExecuteScriptAsync(js);
            }
        }

        // 백그라운드 실행 막음
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            winFormsBrowser.Dispose();
            Application.Current.Shutdown();
        }

        // 확대
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel += 0.15;
            winFormsBrowser.SetZoomLevel(currentZoomLevel);
        }

        // 축소
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel -= 0.15;
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

        // 고대비
        private void HighContrast_Click(object sender, RoutedEventArgs e)
        {
            // 토글 상태 전환
            isHighContrast = !isHighContrast;

            // 고대비 켜질 때는 이미지 필터 원본 보기 상태로 시작
            if (isHighContrast)
            {
                isMediaFilterOff = false;
                MediaFilterButtonText.Text = "사진 고대비 적용";
                MediaFilterToggleButton.Visibility = Visibility.Visible; // 버튼 보이기
            }
            else
            {
                MediaFilterToggleButton.Visibility = Visibility.Collapsed; // 버튼 숨기기
            }

            // 색상 정의
            var bg = isHighContrast ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
            var fg = isHighContrast ? System.Windows.Media.Brushes.Yellow : System.Windows.Media.Brushes.Black;
            var border = isHighContrast ? System.Windows.Media.Brushes.Orange : System.Windows.Media.Brushes.Gray;
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

            // CefSharp 고대비 적용
            var chrome = browserHost.Child as CefSharp.WinForms.ChromiumWebBrowser;
            if (chrome != null && chrome.IsBrowserInitialized)
            {
                /*bool applyMediaFilter = !isMediaFilterOff;*/
                string script = GetHighContrastScript(isHighContrast, applyMediaFilter: false);
                chrome.ExecuteScriptAsync(script);
            }
            // 설정 저장
            Properties.Settings.Default.HighContrastEnabled = isHighContrast;
            Properties.Settings.Default.Save();
        }

        // img, video 포함 고대비 적용
        private string GetHighContrastScript(bool enabled, bool applyMediaFilter)
        {
            return enabled
        ? $@"
(function applyGlobalContrast() {{
  // 이미지 먼저 필터 적용 (선제적)
  const mediaElements = document.querySelectorAll('img, video');
  mediaElements.forEach(el => {{
    el.style.filter = {(applyMediaFilter
        ? "'invert(100%) contrast(200%) grayscale(100%)'"
        : "'invert(100%) hue-rotate(180deg)'")};
    el.style.transition = 'none'; // 깜빡임 방지
  }});

  // 페이지 전체 필터 적용
  document.documentElement.style.filter = 'invert(100%) hue-rotate(180deg)';
  document.documentElement.style.transition = 'none'; // 깜빡임 방지
}})();"
        : @"
(function removeGlobalContrast() {
  document.documentElement.style.filter = '';
  const mediaElements = document.querySelectorAll('img, video');
  mediaElements.forEach(el => {
    el.style.removeProperty('filter');
    el.style.removeProperty('transition');
  });
})();
";
        }

        // 사진 고대비 적용
        private void MediaFilterToggle_Click(object sender, RoutedEventArgs e)
        {
            isMediaFilterOff = !isMediaFilterOff;
            MediaFilterButtonText.Text = isMediaFilterOff ? "사진 고대비 적용" : "사진 원본 보기";

            // 필터 꺼져 있다면 다시 고대비 적용 (고대비가 켜져 있을 때만)
            if (isHighContrast && winFormsBrowser.IsBrowserInitialized)
            {
                string script = GetHighContrastScript(isHighContrast, !isMediaFilterOff);
                winFormsBrowser.ExecuteScriptAsync(script);
            }
        }

        // 폰트 크기 증가
        private void IncreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (currentFontSize < 2.0) // 최대 200%
            {
                currentFontSize += 0.1;
                ApplyFontScale(currentFontSize);
            }
        }

        // 폰트 크기 감소
        private void DecreaseFontSize_Click(object sender, RoutedEventArgs e)
        {
            if (currentFontSize > 0.5) // 최소 50%
            {
                currentFontSize -= 0.1;
                ApplyFontScale(currentFontSize);
            }
        }

        // 폰트 크기
        private void ApplyFontScale(double scale)
        {
            string script = $@"
        (function() {{
            const existingStyle = document.getElementById('custom-font-scale-style');
            if (existingStyle) existingStyle.remove();

            const style = document.createElement('style');
            style.id = 'custom-font-scale-style';
            style.innerHTML = `
                p, span, li, td, th, article, section {{
                    font-size: {scale * 100}% !important;
                }}
            `;
            document.head.appendChild(style);
        }})();
    ";

            winFormsBrowser.ExecuteScriptAsync(script);
        }

        // 홈화면 이동
        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            string homepage = Properties.Settings.Default.HomeUrl;

            if (!string.IsNullOrWhiteSpace(homepage))
            {
                winFormsBrowser.Load(homepage);
            }
            else
            {
                winFormsBrowser.Load("https://www.naver.com");
            }
        }

        // 폰트 변경
        private void ChangeFont_Click(object sender, RoutedEventArgs e)
        {
            var fontDialog = new WinForms.FontDialog();

            if (fontDialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                // 설정 저장
                Properties.Settings.Default.SelectedFontName = fontDialog.Font.Name;
                Properties.Settings.Default.SelectedFontSize = fontDialog.Font.SizeInPoints;
                Properties.Settings.Default.IsFontBold = fontDialog.Font.Bold;
                Properties.Settings.Default.IsFontItalic = fontDialog.Font.Italic;
                Properties.Settings.Default.IsFontCustomized = true;
                Properties.Settings.Default.Save();

                if (this.Owner is MainWindow main)
                {
                    main.ApplyBrowserFont(fontDialog.Font);
                }
            }
        }

        // 즐겨찾기
        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            var favWindow = new FavoriteWindow();
            favWindow.Owner = this;
            favWindow.ShowDialog();
        }

        public string CurrentUrl => winFormsBrowser?.Address;

        public void NavigateToUrl(string url)
        {
            if (!string.IsNullOrWhiteSpace(url))
            {
                winFormsBrowser.Load(url);
            }
        }
    }
}