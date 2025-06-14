using CefSharp;
using CefSharp.WinForms;
using System;
using System.Diagnostics;
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



        // 고대비
        private bool isHighContrast = false;







        public MainWindow()
        {
            InitializeComponent();

            winFormsBrowser = new ChromiumWebBrowser("https://www.naver.com");
            winFormsBrowser.LifeSpanHandler = new CustomLifeSpanHandler();
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
                return;
            }

            string selectedText = "";

            // 1️⃣ 웹브라우저 안에서 선택된 텍스트 가져오기
            var result = await winFormsBrowser.EvaluateScriptAsync("window.getSelection().toString();");
            if (result.Success && result.Result is string browserText && !string.IsNullOrWhiteSpace(browserText))
            {
                selectedText = browserText;
            }
            else
            {
                // 2️⃣ 사이드바 TextBox에서 선택된 텍스트 가져오기 (예: AiSearchResultTextBox)
                if (AiSearchResultTextBox.IsVisible && !string.IsNullOrWhiteSpace(AiSearchResultTextBox.SelectedText))
                {
                    selectedText = AiSearchResultTextBox.SelectedText;
                }
                else if (WhatIsThisPageResultTextBox.IsVisible && !string.IsNullOrWhiteSpace(WhatIsThisPageResultTextBox.SelectedText))
                {
                    selectedText = WhatIsThisPageResultTextBox.SelectedText;
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
                CustomMessageBox.Show("먼저 읽고 싶은 텍스트를 마우스로 선택해 주세요.\n(글자 위에 클릭 후 쭉 움직이기)", this);
            }
        }

        //private async void SpeakButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (isSpeaking)
        //    {
        //        ttsService.Stop();
        //        isSpeaking = false;
        //        SpeakButtonText.Text = "소리로 듣기";
        //        SpeakButtonImage.Source = new BitmapImage(new Uri(IconPlay));
        //    }
        //    else
        //    {
        //        var result = await winFormsBrowser.EvaluateScriptAsync("window.getSelection().toString();");
        //        if (result.Success && result.Result is string selectedText && !string.IsNullOrWhiteSpace(selectedText))
        //        {
        //            ttsService.Speak(selectedText);
        //            isSpeaking = true;
        //            SpeakButtonText.Text = "중단";
        //            SpeakButtonImage.Source = new BitmapImage(new Uri(IconStop));
        //        }
        //        else
        //        {
        //            CustomMessageBox.Show("먼저 읽고 싶은 텍스트를 마우스로 선택해 주세요.\n(글자 위에 클릭 후 쭉 움직이기)", this);
        //        }
        //    }
        //}



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


        private async void SearchOnCurrentPageOrFallback(string keyword)
        {
            string jsKeyword = EscapeForJs(keyword); // 따옴표 등 JS 문자열용 이스케이프

            string jsScript = $@"
        (function() {{
            var input = document.querySelector('input[type=search], input[name*=search], input[id*=search], input[class*=search]');
            if (!input) return 'NO_SEARCH_INPUT_FOUND';

            input.focus();
            input.value = '{jsKeyword}';

            // 이벤트 전파 (change, input 등)
            var events = ['input', 'change', 'keydown', 'keyup', 'keypress'];
            for (let evt of events) {{
                var event = new Event(evt, {{ bubbles: true }});
                input.dispatchEvent(event);
            }}

            // Enter 키 시뮬레이션
            var enterEvent = new KeyboardEvent('keydown', {{
                key: 'Enter',
                keyCode: 13,
                which: 13,
                bubbles: true
            }});
            input.dispatchEvent(enterEvent);

            return 'SEARCH_TRIGGERED';
        }})();
    ";

            var result = await winFormsBrowser.EvaluateScriptAsync(jsScript);
            string response = result?.Result?.ToString() ?? "";

            if (response != "SEARCH_TRIGGERED")
            {
                // 실패 시 네이버 폴백
                string fallbackUrl = $"https://search.naver.com/search.naver?query={Uri.EscapeDataString(keyword)}";
                winFormsBrowser.Load(fallbackUrl);
            }
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
                AiUserQueryTextBox.Foreground = Brushes.Black;
            }
        }

        private void AiUserQueryTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(AiUserQueryTextBox.Text))
            {
                AiUserQueryTextBox.Text = "13살 손녀가 좋아할 선물 추천해줘";
                AiUserQueryTextBox.Foreground = Brushes.Gray;
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

            var sb = new StringBuilder();
            var lines = raw.Split('\n');

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                // 리스트 항목 또는 숫자 리스트
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("•") || trimmed.StartsWith("*") ||
                    System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+[\).]\s"))
                {
                    sb.AppendLine("  " + trimmed);
                }
                else if (string.IsNullOrWhiteSpace(trimmed))
                {
                    sb.AppendLine(); // 공백 줄 유지
                }
                else
                {
                    sb.AppendLine(trimmed);
                }
            }

            return sb.ToString();
        }


    }
}
