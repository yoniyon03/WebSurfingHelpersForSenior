using System;
using System.Speech.Synthesis;

namespace WebSurfingHelpersForSenior
{
    public class TTSService
    {
        private readonly SpeechSynthesizer synthesizer = new SpeechSynthesizer();

        public event EventHandler SpeakCompleted;

        public TTSService()
        {
            synthesizer.SpeakCompleted += (s, e) =>
            {
                SpeakCompleted?.Invoke(this, EventArgs.Empty);
            };
        }

        public void Speak(string text)
        {
            synthesizer.SpeakAsyncCancelAll();
            synthesizer.SpeakAsync(text);
        }

        public void Stop()
        {
            synthesizer.SpeakAsyncCancelAll();
        }

        public void SetRate(int rate) => synthesizer.Rate = rate;     // -10 ~ 10
        public void SetVolume(int volume) => synthesizer.Volume = volume; // 0 ~ 100
    }
}
