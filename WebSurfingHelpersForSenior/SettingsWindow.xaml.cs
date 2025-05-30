using System;
using System.Windows;
using System.Windows.Threading;

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
    }
}
