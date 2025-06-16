using System;
using System.Windows;

namespace WebSurfingHelpersForSenior
{
    public partial class SettingsWindow : Window
    {
        private readonly TTSService tts;

        public SettingsWindow(TTSService ttsService)
        {
            InitializeComponent();
            tts = ttsService;
        }

        private void ShowVoiceSettings_Click(object sender, RoutedEventArgs e)
        {
            var voiceWindow = new VoiceSettingsWindow(tts);
            voiceWindow.Owner = this;
            voiceWindow.ShowDialog();
        }


        private void SetHomeUrl_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SetHomePageWindow();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                Properties.Settings.Default.HomeUrl = dialog.SelectedUrl;
                Properties.Settings.Default.Save();
                CustomMessageBox.Show("홈페이지가 설정되었습니다.", "확인", MessageBoxImage.Information, this);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
