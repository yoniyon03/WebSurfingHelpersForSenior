using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using Microsoft.VisualBasic;

namespace WebSurfingHelpersForSenior
{
    public partial class SettingsWindow : Window
    {
        private readonly TTSService tts;
        private DispatcherTimer debounceTimer;

        public SettingsWindow(TTSService ttsService)
        {
            InitializeComponent();  // XAML 요소 연결

            tts = ttsService;

            debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            debounceTimer.Tick += DebounceTimer_Tick;

            // ✅ 슬라이더 접근은 Loaded 시점으로 연기
            Loaded += SettingsWindow_Loaded;
        }

        // ✅ 여기서만 슬라이더에 안전하게 접근
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (RateSlider != null && VolumeSlider != null)
            {
                RateSlider.Value = 0;
                VolumeSlider.Value = 100;
            }
            else
            {
                MessageBox.Show("슬라이더가 초기화되지 않았습니다.");
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return; // 창이 완전히 로드되기 전엔 무시

            debounceTimer.Stop();
            debounceTimer.Start();
        }

        private void DebounceTimer_Tick(object sender, EventArgs e)
        {
            debounceTimer.Stop();

            try
            {
                tts.SetRate((int)RateSlider.Value);
                tts.SetVolume((int)VolumeSlider.Value);
                tts.Speak("안녕하세요");
            }
            catch (Exception ex)
            {
                MessageBox.Show("TTS 테스트 실패: " + ex.Message);
            }
        }

        // 홈 화면 설정
        private void SetHomeUrl_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SetHomePageWindow(); // 홈 설정 팝업
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                Properties.Settings.Default.HomeUrl = dialog.SelectedUrl;
                Properties.Settings.Default.Save();
                CustomMessageBox.Show("홈페이지가 설정되었습니다.", "확인", MessageBoxImage.Information);
            }
        }
    }
}
