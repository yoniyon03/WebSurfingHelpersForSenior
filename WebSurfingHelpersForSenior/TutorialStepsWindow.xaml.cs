using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WebSurfingHelpersForSenior
{
    public partial class TutorialStepsWindow : Window
    {
        private int currentStep = 0;

        private List<TutorialStep> steps = new List<TutorialStep>
        {
            new TutorialStep("1) 홈 화면으로 바로가기", "🏠 홈 버튼을 누르면 언제든지 처음 화면으로 돌아갈 수 있어요.\n원래 보던 화면이 헷갈리면 이 버튼으로 다시 시작해보세요.", "/Assets/home.png"),
            new TutorialStep("2) 즐겨찾기", "자주 방문하는 사이트는 ★ 즐겨찾기에 등록해두면 나중에 한 번에 찾아갈 수 있어요.", "/Assets/favorites.png"),
            new TutorialStep("2) 즐겨찾기", "클릭하고 나오는 부분 캡처해서 하면 좋을듯", "/Assets/favorites.png"),
            new TutorialStep("3) 뒤로 가기 / 앞으로 가기", "뒤로 가기(◀)를 누르면 방금 전 페이지로 돌아가요.\n앞으로 가기(▶)를 누르면 다시 돌아올 수 있어요.", "/Assets/goBackGoForward.png"),
            new TutorialStep("4) 소리로 듣기", "글 보기가 힘들 땐 🎧 소리로 듣기 버튼을 눌러보세요.\n음성으로 천천히 또박또박 읽어줘서 편해요.", "/Assets/sound.png"),
            new TutorialStep("5) 설정", "홈 화면과 음성을 내 스타일에 맞게 바꿔보세요.\n설정 버튼에서 쉽게 고를 수 있어요.", "/Assets/setting.png"),
        };

        public TutorialStepsWindow()
        {
            InitializeComponent();
            LoadStep(currentStep);
        }

        private void LoadStep(int index)
        {
            if (index < 0 || index >= steps.Count) return;

            StepTitle.Text = steps[index].Title;
            StepDescription.Text = steps[index].Description;

            string imagePath = $"pack://application:,,,{steps[index].ImagePath}";
            StepImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        }


        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep < steps.Count - 1)
            {
                currentStep++;
                LoadStep(currentStep);
            }
        }

        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentStep > 0)
            {
                currentStep--;
                LoadStep(currentStep);
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.SkipTutorial = DontShowAgainCheck.IsChecked == true;
            Properties.Settings.Default.Save();
            this.Close();
        }

        private class TutorialStep
        {
            public string Title { get; }
            public string Description { get; }
            public string ImagePath { get; }

            public TutorialStep(string title, string desc, string imagePath)
            {
                Title = title;
                Description = desc;
                ImagePath = imagePath;
            }
        }
    }
}
