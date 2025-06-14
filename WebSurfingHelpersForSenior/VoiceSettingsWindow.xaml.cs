using System;
using System.Windows;
using System.Windows.Threading;

namespace WebSurfingHelpersForSenior
{
    public partial class VoiceSettingsWindow : Window
    {
        private readonly TTSService tts;
        private DispatcherTimer debounceTimer;

        public VoiceSettingsWindow(TTSService ttsService)
        {
            InitializeComponent();
            tts = ttsService;

            debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            debounceTimer.Tick += DebounceTimer_Tick;

            Loaded += VoiceSettingsWindow_Loaded;
        }

        private void VoiceSettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            RateSlider.Value = 0;
            VolumeSlider.Value = 100;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
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
                CustomMessageBox.Show("TTS 실패: " + ex.Message, "오류", MessageBoxImage.Error, this);
            }
        }
    }
}
