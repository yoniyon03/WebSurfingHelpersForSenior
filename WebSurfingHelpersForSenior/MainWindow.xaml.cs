using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms.Integration;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
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

        // 소리로 듣기, 중단을 위한 
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

            // 설정 복원
            currentZoomLevel = Properties.Settings.Default.ZoomLevel;
            currentFontSize = Properties.Settings.Default.FontScale;
            isMediaFilterOff = Properties.Settings.Default.MediaFilterOff;
            isHighContrast = Properties.Settings.Default.HighContrastEnabled;

            // 고대비 WPF UI 복원
            if (isHighContrast)
            {
                MediaFilterToggleButton.Visibility = Visibility.Visible;
                MediaFilterButtonText.Text = isMediaFilterOff ? "사진 고대비 적용" : "사진 원본 보기";

                var bg = System.Windows.Media.Brushes.Black;
                var fg = System.Windows.Media.Brushes.Yellow;
                var border = System.Windows.Media.Brushes.Orange;
                var weight = FontWeights.Bold;

                this.Background = bg;

                foreach (var ctrl in LogicalTreeHelper.GetChildren(this))
                {
                    if (ctrl is Control c)
                    {
                        c.Background = bg;
                        c.Foreground = fg;
                        c.FontWeight = weight;

                        if (c is Button || c is ToggleButton)
                            c.BorderBrush = border;
                    }
                }
            }

            // 주소 설정 (비우고 먼저 생성)
            string url = Properties.Settings.Default.LastVisitedUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                url = "https://www.naver.com";
            }

            // 브라우저 생성 및 설정
            winFormsBrowser = new ChromiumWebBrowser(url);
            winFormsBrowser.LifeSpanHandler = new CustomLifeSpanHandler(); // 팝업차단
            browserHost.Child = winFormsBrowser;

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


            //  웹페이지 로딩 완료 후 AI 제안 체크 연결
            winFormsBrowser.FrameLoadEnd += async (sender, args) =>
            {
                if (args.Frame.IsMain)
                {
                    Dispatcher.Invoke(() =>
                    {
                        CheckSmartSuggestion();
                    });


                    // 관련 키워드 추천 창이 열려 있을 때만 호출
                    if (AiKeywordPagePanel.Visibility == Visibility.Visible)
                    {
                        await Dispatcher.InvokeAsync(() =>
                        {
                            RecommendKeywordsButton_Click(sender, null); // 키워드 추천 메서드 호출
                        });
                    }
                }
            };


            // 음성 완료 이벤트 등록
            ttsService.SpeakCompleted += TtsService_SpeakCompleted;

            this.Closed += MainWindow_Closed;




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
    style.innerHTML = `
        * {{
            font-family: '{fontName.Name}' !important;
            font-weight: {fontWeight} !important;
            font-style: {fontStyle} !important;
            font-size: {fontSize} !important;
        }}
    `;
    document.head.appendChild(style);
}})();
";
                winFormsBrowser.ExecuteScriptAsync(js);
            }
        }

        // 프로그래밍 종료 시 마지막 주소 저장
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(winFormsBrowser.Address))
            {
                Properties.Settings.Default.LastVisitedUrl = winFormsBrowser.Address;
            }

            Properties.Settings.Default.Save();
            winFormsBrowser.Dispose();
            Application.Current.Shutdown();
        }

        // 확대
        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel += 0.15;
            winFormsBrowser.SetZoomLevel(currentZoomLevel);

            Properties.Settings.Default.ZoomLevel = currentZoomLevel;
            Properties.Settings.Default.Save();
        }

        // 축소
        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            currentZoomLevel -= 0.15;
            winFormsBrowser.SetZoomLevel(currentZoomLevel);

            Properties.Settings.Default.ZoomLevel = currentZoomLevel;
            Properties.Settings.Default.Save();
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
                return;
            }

            string selectedText = "";

            // 1️⃣ 웹브라우저 안에서 선택된 텍스트 가져오기
            string jsGetSelectedText = @"
(() => {
  try {
    const sel = window.getSelection();
    if (sel && sel.toString().trim().length > 0) {
      return sel.toString().trim();
    }

    const iframes = document.querySelectorAll('iframe');
    for (const iframe of iframes) {
      try {
        const iframeSel = iframe.contentWindow?.getSelection();
        if (iframeSel && iframeSel.toString().trim().length > 0) {
          return iframeSel.toString().trim();
        }
      } catch (e) {
        continue;
      }
    }

    return '';
  } catch (e) {
    return '';
  }
})()
";

            var browserResult = await winFormsBrowser.EvaluateScriptAsync(jsGetSelectedText);

            if (browserResult.Success && browserResult.Result is string browserText && !string.IsNullOrWhiteSpace(browserText))
            {
                selectedText = browserText;
            }

            // 2️⃣ 사이드바 TextBox 중 선택된 텍스트 가져오기
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                foreach (var child in LogicalTreeHelper.GetChildren(this))
                {
                    if (child is DependencyObject parent)
                    {
                        var textBoxes = FindVisualChildren<System.Windows.Controls.TextBox>(parent);
                        foreach (var tb in textBoxes)
                        {
                            if (tb.IsVisible && !string.IsNullOrWhiteSpace(tb.SelectedText))
                            {
                                selectedText = tb.SelectedText;
                                break;
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(selectedText)) break;
                }
            }


            // 3️⃣ 그래도 텍스트가 없다면, 웹페이지 "본문" 추출해서 읽기
            if (string.IsNullOrWhiteSpace(selectedText))
            {
                string jsMainTextScript = @"
(() => {
  function getMainText() {
    const mainTags = ['main', 'article', 'section'];
    for (const tag of mainTags) {
      const el = document.querySelector(tag);
      if (el && el.innerText.trim().length > 300) {
        return el.innerText.trim();
      }
    }

    // 텍스트가 많은 div 찾기
    const divs = Array.from(document.querySelectorAll('div')).map(div => ({
      el: div,
      length: (div.innerText || '').trim().length
    }));
    divs.sort((a, b) => b.length - a.length);

    if (divs.length > 0 && divs[0].length > 300) {
      return divs[0].el.innerText.trim();
    }

    // fallback
    return document.body.innerText.trim();
  }

  return getMainText();
})()
";

                var fullTextResult = await winFormsBrowser.EvaluateScriptAsync(jsMainTextScript);
                if (fullTextResult.Success && fullTextResult.Result is string fullText && !string.IsNullOrWhiteSpace(fullText))
                {
                    selectedText = fullText;
                }
            }


            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                ttsService.Speak(selectedText);
                isSpeaking = true;
                SpeakButtonText.Text = "중단";
                SpeakButtonImage.Source = new BitmapImage(new Uri(IconStop));
            }
            else
            {
                CustomMessageBox.Show("읽을 텍스트가 없어요. 읽고 싶은 텍스트를 마우스로 선택해 주세요.\n(글자 위에 클릭 후 쭉 움직이기", this);
            }
        }


        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
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

            Properties.Settings.Default.MediaFilterOff = isMediaFilterOff;
            Properties.Settings.Default.Save();
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

            Properties.Settings.Default.FontScale = scale;
            Properties.Settings.Default.Save();
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

                ApplyBrowserFont(fontDialog.Font);
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

        // AI 부분
        private async void WhatIsThisPageButton_Click(object sender, RoutedEventArgs e)
        {
            var browser = winFormsBrowser;
            var url = browser?.Address ?? "";

            if (string.IsNullOrEmpty(url))
            {
                WhatIsThisPageResultTextBox.Text = "현재 페이지 주소를 확인할 수 없어요.";
                return;
            }

            var gemini = new GeminiService();

            WhatIsThisPagePanel.Visibility = Visibility.Visible;
            AiMainMenuPanel.Visibility = Visibility.Collapsed;
            AiSearchPanel.Visibility = Visibility.Collapsed;

            WhatIsThisLoadingBar.Visibility = Visibility.Visible;
            WhatIsThisPageResultTextBox.Text = "AI가 설명을 가져오는 중이에요. 잠시만 기다려주세요...";

            await Dispatcher.Yield(); // ✅ UI 반영 보장

            try
            {
                string response = await gemini.AskWhatIsThisPage(url);
                WhatIsThisPageResultTextBox.Text = FormatAiText(response);
            }
            catch (Exception ex)
            {
                WhatIsThisPageResultTextBox.Text = "AI 응답을 가져오지 못했어요. 잠시 후 다시 시도해 주세요.\n\n오류: " + ex.Message;
            }
            finally
            {
                WhatIsThisLoadingBar.Visibility = Visibility.Collapsed;
            }
        }




        private async void SummarizePageButton_Click(object sender, RoutedEventArgs e)
        {
            if (winFormsBrowser == null || !winFormsBrowser.IsBrowserInitialized)
            {
                MessageBox.Show("브라우저가 아직 준비되지 않았어요.");
                return;
            }

            // 페이지 전체 텍스트 추출
            var result = await winFormsBrowser.EvaluateScriptAsync("document.documentElement.innerText");

            if (!result.Success || !(result.Result is string pageText) || string.IsNullOrWhiteSpace(pageText))
            {
                CustomMessageBox.Show("페이지에서 요약할 텍스트를 찾지 못했어요.");
                return;
            }

            SummarizeLoadingBar.Visibility = Visibility.Visible; // ✅ 로딩 시작

            try
            {
                var gemini = new GeminiService();
                string summary = await gemini.SummarizePageText(pageText);
                SummarizedTextBox.Text = FormatAiText(summary);
            }
            catch (Exception ex)
            {
                SummarizedTextBox.Text = "요약 중 오류 발생: " + ex.Message;
            }
            finally
            {
                SummarizeLoadingBar.Visibility = Visibility.Collapsed; // ✅ 로딩 종료
            }

            AiMainMenuPanel.Visibility = Visibility.Collapsed;
            SummarizePagePanel.Visibility = Visibility.Visible;

        }


        private async void ExplainWordButton_Click(object sender, RoutedEventArgs e)
        {
            if (winFormsBrowser == null || !winFormsBrowser.IsBrowserInitialized)
            {
                CustomMessageBox.Show("브라우저가 아직 준비되지 않았어요.");
                return;
            }

            var result = await winFormsBrowser.EvaluateScriptAsync("window.getSelection().toString();");

            if (!result.Success || !(result.Result is string selectedText) || string.IsNullOrWhiteSpace(selectedText))
            {
                CustomMessageBox.Show("먼저 뜻을 알고 싶은 단어를 마우스로 선택해주세요.");
                return;
            }

            ExplainWordLoadingBar.Visibility = Visibility.Visible;

            var word = selectedText.Trim();

            try
            {
                var gemini = new GeminiService();
                string explanation = await gemini.ExplainWordMeaning(word);

                // 단어 뜻을 표시
                WordExplanationTextBox.Text = FormatAiText(explanation);

                // 사이드바 패널 표시
                AiMainMenuPanel.Visibility = Visibility.Collapsed;
                ExplainWordPanel.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                WordExplanationTextBox.Text = "단어 뜻을 가져오는 중 오류가 발생했어요:\n\n" + ex.Message;
            }
            finally
            {
                ExplainWordLoadingBar.Visibility = Visibility.Collapsed; // ✅ 로딩 종료
            }
        }


        private async void ExplainMoreButton_Click(object sender, RoutedEventArgs e)
        {
            string topic = AiExplanationTitle.Text?.Replace("📝", "").Trim();
            if (string.IsNullOrEmpty(topic)) return;

            // ✅ 일단 로딩 UI 표시 먼저
            AiExplanationLoadingBar.Visibility = Visibility.Visible;
            AiExplanationContent.Text = "AI가 천천히 설명을 준비 중이에요...";

            //// ✅ 렌더링 보장
            //await Dispatcher.Yield();

            try
            {
                var gemini = new GeminiService();
                string simplified = await gemini.GetRecommendation(
                    $"'{topic}'에 대해 시니어가 이해하기 어려워해. 더 천천히, 더 쉽게 풀어서 예시와 함께 설명해줘."
                );

                AiExplanationContent.Text = FormatAiText(simplified);
            }
            catch (Exception ex)
            {
                AiExplanationContent.Text = $"추가 설명 중 오류 발생: {ex.Message}";
            }
            finally
            {
                AiExplanationLoadingBar.Visibility = Visibility.Collapsed;
            }

            // ✅ 작업 끝난 후 패널 전환
            AiMainMenuPanel.Visibility = Visibility.Collapsed;
            AiSmartSuggestionPanel.Visibility = Visibility.Collapsed;
            AiExplanationPanel.Visibility = Visibility.Visible;
        }


        private async void MoreExplanation_Click(object sender, RoutedEventArgs e)
        {
            string topic = AiExplanationTitle.Text?.Replace("📝", "").Trim();
            if (string.IsNullOrEmpty(topic)) return;

            // ✅ 먼저 패널 전환
            AiMainMenuPanel.Visibility = Visibility.Collapsed;
            AiSmartSuggestionPanel.Visibility = Visibility.Collapsed;
            AiExplanationPanel.Visibility = Visibility.Visible;

            // ✅ 로딩바 표시
            AiExplanationLoadingBar.Visibility = Visibility.Visible;
            AiExplanationContent.Text = "AI가 천천히 설명을 준비 중이에요...";

            await Dispatcher.Yield(); // ✅ UI 반영 먼저 시킴 (로딩바 보이게)

            try
            {
                var gemini = new GeminiService();
                string more = await gemini.GetRecommendation(
                    $"'{topic}'에 대해 시니어가 여전히 이해하기 어려워해. 더 천천히, 더 쉽게 풀어서 설명해줘."
                );

                AiExplanationContent.Text = FormatAiText(more);
            }
            catch (Exception ex)
            {
                AiExplanationContent.Text = "추가 설명 중 오류 발생: " + ex.Message;
            }
            finally
            {
                AiExplanationLoadingBar.Visibility = Visibility.Collapsed;
            }
        }








        private async void AiHelpSearchButton_Click(object sender, RoutedEventArgs e)
        {
            string query = AiUserQueryTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(query))
            {
                AiSearchResultTextBox.Text = "무엇이 궁금하신가요?\n검색어를 입력해 주세요.";
                AiSearchResultPanel.Visibility = Visibility.Visible;
                AiMainMenuPanel.Visibility = Visibility.Collapsed;
                AiSearchPanel.Visibility = Visibility.Collapsed;
                return;
            }

            AiSearchLoadingBar.Visibility = Visibility.Visible;
            AiSearchResultPanel.Visibility = Visibility.Visible;
            AiMainMenuPanel.Visibility = Visibility.Collapsed;
            AiSearchPanel.Visibility = Visibility.Collapsed;
            AiSearchResultTextBox.Text = "AI가 답변을 생성 중이에요. 잠시만 기다려 주세요...";
            await Dispatcher.Yield(DispatcherPriority.Background);

            try
            {
                var gemini = new GeminiService();
                string response = await gemini.AnswerForUserQuery(query);
                AiSearchResultTextBox.Text = FormatAiText(response);
            }
            catch (Exception ex)
            {
                AiSearchResultTextBox.Text = "AI 응답을 가져오는 중 오류가 발생했어요: " + ex.Message;
            }
            finally
            {
                AiSearchLoadingBar.Visibility = Visibility.Collapsed;
            }
        }



        // 'AI가 대신 찾아줘요' 클릭 시: 검색 UI로 전환
        private void ShowAiSearchPanel_Click(object sender, RoutedEventArgs e)
        {
            AiMainMenuPanel.Visibility = Visibility.Collapsed;
            AiSearchPanel.Visibility = Visibility.Visible;
            AiSearchResultTextBox.Text = ""; // 초기화
        }



        // 뒤로가기 클릭 시: 다시 메뉴로 복귀 (기능별)
        private void BackToAiMenu_Click1(object sender, RoutedEventArgs e)
        {
            WhatIsThisPagePanel.Visibility = Visibility.Collapsed;
            AiMainMenuPanel.Visibility = Visibility.Visible;
        }


        private void BackToAiMenu_Click2(object sender, RoutedEventArgs e)
        {
            AiSearchPanel.Visibility = Visibility.Collapsed;
            AiSearchResultPanel.Visibility = Visibility.Collapsed; // 이 줄 추가
            AiMainMenuPanel.Visibility = Visibility.Visible;
        }


        private void BackToAiMenu_Click3(object sender, RoutedEventArgs e)
        {
            AiKeywordPagePanel.Visibility = Visibility.Collapsed;
            AiMainMenuPanel.Visibility = Visibility.Visible;
        }

        private void BackToAiMenu_Click4(object sender, RoutedEventArgs e)
        {
            AiExplanationPanel.Visibility = Visibility.Collapsed;
            AiMainMenuPanel.Visibility = Visibility.Visible;
        }

        // 본문 내용 요약 뒤로가기 버튼 클릭 시
        private void BackToAiMenu_Click5(object sender, RoutedEventArgs e)
        {
            SummarizePagePanel.Visibility = Visibility.Collapsed;
            AiMainMenuPanel.Visibility = Visibility.Visible;
        }

        // 단어 뜻 알려주기 뒤로가기 버튼 클릭 시
        private void BackToAiMenu_Click6(object sender, RoutedEventArgs e)
        {
            ExplainWordPanel.Visibility = Visibility.Collapsed;
            AiMainMenuPanel.Visibility = Visibility.Visible;
        }





        // 키워드로 웹서핑 유도
        private async void RecommendKeywordsButton_Click(object sender, RoutedEventArgs e)
        {
            AiMainMenuPanel.Visibility = Visibility.Collapsed;
            AiSearchPanel.Visibility = Visibility.Collapsed;
            WhatIsThisPagePanel.Visibility = Visibility.Collapsed;
            AiKeywordPagePanel.Visibility = Visibility.Visible;

            WhatIsThisPageResultTextBox.Text = "AI가 추천 키워드를 가져오는 중이에요...";
            AiKeywordLoadingBar.Visibility = Visibility.Visible; // ✅ 로딩 시작

            var gemini = new GeminiService();

            var result = await winFormsBrowser.EvaluateScriptAsync("document.documentElement.innerText");
            if (!result.Success || !(result.Result is string pageText) || string.IsNullOrWhiteSpace(pageText))
            {
                WhatIsThisPageResultTextBox.Text = "페이지 본문을 읽을 수 없어요.";
                AiKeywordLoadingBar.Visibility = Visibility.Collapsed; // ✅ 로딩 종료
                return;
            }

            try
            {
                var keywords = await gemini.RecommendKeywordsFromPageText(pageText);

                if (keywords == null || keywords.Count == 0)
                {
                    WhatIsThisPageResultTextBox.Text = "추천할 키워드를 찾지 못했어요.";
                }
                else
                {
                    WhatIsThisPageResultTextBox.Text = "이 페이지와 관련된 추천 키워드입니다:";

                    AiKeywordPanel.Children.Clear();
                    foreach (var keyword in keywords)
                    {
                        var btn = new Button
                        {
                            Content = $"🔍 {keyword}",
                            Margin = new Thickness(5),
                            FontSize = 14,
                            Tag = keyword
                        };
                        btn.Click += AiKeywordButton_Click;
                        AiKeywordPanel.Children.Add(btn);
                    }
                    AiKeywordPanel.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                WhatIsThisPageResultTextBox.Text = "추천 키워드를 가져오는 중 오류가 발생했어요: " + ex.Message;
            }
            finally
            {
                AiKeywordLoadingBar.Visibility = Visibility.Collapsed; // ✅ 로딩 종료
            }
        }





        private void AiKeywordButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string keyword)
            {
                try
                {
                    // 선택된 키워드로 검색 실행 (시니어 친화적 설명)
                    //string searchUrl = $"https://search.naver.com/search.naver?query={Uri.EscapeDataString(keyword)}";
                    //winFormsBrowser.Load(searchUrl);
                    SearchOnCurrentPageOrFallback(keyword);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"키워드 검색 중 오류 발생: {ex.Message}");
                }
            }
        }


        private void SearchOnCurrentPageOrFallback(string keyword)
        {
            string baseUrl = Properties.Settings.Default.HomeUrl;

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                CustomMessageBox.Show("홈 주소가 설정되지 않았습니다. 설정에서 먼저 지정해 주세요.", "알림", MessageBoxImage.Warning, this);
                return;
            }

            string lowerBase = baseUrl.ToLower();
            string finalUrl;

            if (lowerBase.Contains("naver.com"))
            {
                finalUrl = $"https://search.naver.com/search.naver?query={Uri.EscapeDataString(keyword)}";
            }
            else if (lowerBase.Contains("daum.net"))
            {
                finalUrl = $"https://search.daum.net/search?w=tot&q={Uri.EscapeDataString(keyword)}";
            }
            else if (lowerBase.Contains("google.com"))
            {
                finalUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(keyword)}";
            }
            else
            {
                CustomMessageBox.Show("지원하지 않는 홈 주소입니다.\n네이버, 다음, 구글 중에서 선택해 주세요.", "알림", MessageBoxImage.Warning, this);
                return;
            }

            winFormsBrowser.Load(finalUrl);
        }




        private string EscapeForJs(string text)
        {
            return text.Replace("\\", "\\\\")
                       .Replace("'", "\\'")
                       .Replace("\"", "\\\"")
                       .Replace("\r", "")
                       .Replace("\n", "");
        }




        private void AiUserQueryTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (AiUserQueryTextBox.Text == "13살 손녀가 좋아할 선물 추천해줘")
            {
                AiUserQueryTextBox.Text = "";
                AiUserQueryTextBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void AiUserQueryTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AiUserQueryTextBox.Text))
            {
                AiUserQueryTextBox.Text = "13살 손녀가 좋아할 선물 추천해줘";
                AiUserQueryTextBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }


        private async void CheckSmartSuggestion()
        {
            var gemini = new GeminiService();

            var result = await winFormsBrowser.EvaluateScriptAsync("document.documentElement.innerText");

            if (!result.Success || !(result.Result is string pageText) || string.IsNullOrWhiteSpace(pageText))
                return;

            try
            {
                string suggestion = await gemini.GenerateSuggestionFromText(pageText);
                if (!string.IsNullOrWhiteSpace(suggestion))
                {
                    AiSmartSuggestionText.Text = $"❓ {suggestion.Trim()} ";
                    AiSmartSuggestionPanel.Visibility = Visibility.Visible;
                    AiSuggestionExplanationTextBox.Visibility = Visibility.Collapsed;
                    AiSuggestionExplanationTextBox.Text = "";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("AI 제안 실패: " + ex.Message);
            }
        }


        private async void ExplainSuggestion_Click(object sender, RoutedEventArgs e)
        {
            string topic = AiSmartSuggestionText.Text?.Trim();
            if (string.IsNullOrEmpty(topic)) return;

            AiExplanationLoadingBar.Visibility = Visibility.Visible; // ✅ 로딩 시작

            try
            {
                var gemini = new GeminiService();
                string explanation = await gemini.ExplainTopicStepByStep(topic);
                Debug.WriteLine("📝 설명 전체 텍스트:\n" + explanation);

                // 패널 전환
                AiMainMenuPanel.Visibility = Visibility.Collapsed;
                AiSmartSuggestionPanel.Visibility = Visibility.Collapsed;
                AiExplanationPanel.Visibility = Visibility.Visible;

                AiExplanationTitle.Text = $"📝 {topic}";
                AiExplanationContent.Text = explanation;

            }
            catch (Exception ex)
            {
                AiExplanationContent.Text = "오류가 발생했어요: " + ex.Message;
            }
            finally
            {
                AiExplanationLoadingBar.Visibility = Visibility.Collapsed; // ✅ 로딩 종료
            }
        }









        private void CloseSuggestion_Click(object sender, RoutedEventArgs e)
        {
            AiSmartSuggestionPanel.Visibility = Visibility.Collapsed;
        }



        private string FormatAiText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            // 1️⃣ HTML 줄바꿈을 실제 줄바꿈으로 치환
            raw = raw.Replace("<br>", "\n").Replace("<br/>", "\n").Replace("<br />", "\n");

            var sb = new StringBuilder();
            var lines = raw.Split('\n');

            foreach (var line in lines)
            {
                string trimmed = System.Net.WebUtility.HtmlDecode(line.Trim());

                if (trimmed.StartsWith("- ") || trimmed.StartsWith("•") || trimmed.StartsWith("*") ||
                    System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+[\).]\s"))
                {
                    sb.AppendLine("  " + trimmed);
                }
                else if (string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine(); // 빈 줄 유지
                }
                else
                {
                    sb.AppendLine(trimmed);
                }
            }

            return sb.ToString();
        }


        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var stepsWindow = new TutorialStepsWindow(fromHelp: true);
            stepsWindow.Owner = this;
            stepsWindow.ShowDialog();
        }

    }

}