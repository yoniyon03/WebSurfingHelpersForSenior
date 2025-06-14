using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace WebSurfingHelpersForSenior
{
    public partial class TutorialStepsWindow : Window
    {
        private int currentStep = 0;
        private bool skipTutorialOnNextLaunch = false;
        private bool isFromHelpButton = false;

        private List<TutorialStep> steps = new List<TutorialStep>
        {
            new TutorialStep("1) 홈 화면으로 바로가기", "[🏠 홈] 버튼을 누르면 언제든지 처음 화면으로 돌아갈 수 있어요.\n원래 보던 화면이 헷갈리면 이 버튼으로 다시 시작해보세요.", "/Assets/home.png"),
            new TutorialStep("2-1) 즐겨찾기", "자주 방문하는 사이트는 [★ 즐겨찾기]에 등록해두면 나중에 한 번에 찾아갈 수 있어요.", "/Assets/favorites.png"),
            new TutorialStep("2-2) 즐겨찾기", "[★ 즐겨찾기]를 누르면 나오는 화면이에요.\n[현재 페이지 즐겨찾기 추가]를 누르면 지금 보고 있는 사이트를 저장할 수 있어요.\n더이상 자주 보지 않을 필요 없는 즐겨찾기는 선택 후 [선택한 즐겨찾기 삭제]를 누르면 지울 수 있어요.\n나중에 다시 보고 싶을 땐, 목록에서 원하는 즐겨찾기를 선택하고 [선택한 사이트 바로가기]를 누르면 바로 열 수 있답니다.", "/Assets/favorites-1.png"),
            new TutorialStep("3) 뒤로 가기 / 앞으로 가기", "[뒤로 가기(◀)]를 누르면 방금 전 페이지로 돌아가요.\n[앞으로 가기(▶)]를 누르면 다시 돌아올 수 있어요.", "/Assets/goBackGoForward.png"),
            new TutorialStep("4) 소리로 듣기", "글 보기가 힘들 땐 [🎧 소리로 듣기] 버튼을 눌러보세요.\n음성으로 천천히 또박또박 읽어줘서 편해요.\n원하는 글자를 마우스로 선택해 버튼을 눌러보세요.", "/Assets/sound.png"),
            new TutorialStep("5-1) 설정", "홈 화면과 음성을 내 스타일에 맞게 바꿔보세요.\n[설정] 버튼에서 쉽게 고를 수 있어요.", "/Assets/setting.png"),
            new TutorialStep("5-2) 설정", "[설정]을 누르면 나오는 화면이에요.\n여기서 음성과 홈 화면을 내게 맞게 바꿀 수 있어요.\n필요한 설정을 하고 [닫기] 버튼을 눌러주세요.", "/Assets/setting-1.png"),
            new TutorialStep("5-3) 설정", "방금의 [설정] 화면에서 [음성 설정]을 누르면 나오는 화면이에요.\n듣기 편한 음성 속도와 크기로 조절해보세요.", "/Assets/soundSetting.png"),
            new TutorialStep("6-1) AI 도우미", "오른쪽에 있는 네모칸은 AI 도우미예요.\n인터넷을 보다가 어려운 부분이 있으면, 여기에 있는 버튼을 눌러서 도움을 받을 수 있어요.", "/Assets/UsingAI.png"),
            new TutorialStep("6-2) AI 도우미 - 이 페이지 뭐 하는 곳이야?", "지금 보고 있는 사이트가 어떤 곳인지 잘 모르겠다면 이 버튼을 눌러보세요.\nAI가 쉽게 설명해줘요.", "/Assets/whatIsThisPage.png"),
            new TutorialStep("6-3) AI 도우미 - 본문 내용 요약해서 보기", "뉴스나 글이 너무 길어서 읽기 힘든 적 없으셨나요?\n이 버튼을 누르면 중요한 내용만 쏙쏙 요약해서 보여드려요.", "/Assets/summation.png"),
            new TutorialStep("6-4) AI 도우미 - 단어 뜻 알려줘", "모르는 외국어나 말이 나왔을 때 마우스로 선택하고 이 버튼을 눌러보세요.\nAI가 단어의 뜻을 쉽게 설명해줘요.", "/Assets/word.png"),
            new TutorialStep("6-5) AI 도우미 - AI가 대신 찾아줘요", "직접 검색하기 어렵거나 어디서 찾아야 할지 모를 땐 AI가 대신 찾아드려요.\n이 버튼을 눌러 궁금한 걸 물어보세요.", "/Assets/AiSearch.png"),
            new TutorialStep("6-6) AI 도우미 - 관련 키워드 추천받기", "지금 보고 있는 내용과 관련된 주제가 궁금할 땐, 이 버튼을 눌러보세요.\n다양한 검색어들을 추천해주고 선택하면 직접 설정한 홈 화면에서 자동으로 검색해줘요.", "/Assets/Keyword.png"),
            new TutorialStep("6-7) AI 도우미 - AI의 제안", "AI가 먼저 필요한 걸 물어볼 수도 있어요.\n궁금한 얘기를 AI가 해준다면 [자세히 알려줘요] 버튼을 눌러 설명을 들어보세요.", "/Assets/suggestion.png"),
            new TutorialStep("7) 고대비 화면으로 보기", "[고대비 화면으로 보기]를 누르면 배경은 어두워지고, 글씨는 더 또렷하게 보여요.\n글 읽기가 어렵거나 눈이 아플 때 사용해보세요.", "/Assets/highContrast.png"),
            new TutorialStep("8) 사진 고대비 적용", "글씨뿐만 아니라 사진도 어둡게 보고싶을 땐 [사진 고대비 적용] 버튼을 눌러보세요.\n이 버튼을 누르면 화면에 있는 사진도 고대비로 변해요.", "/Assets/highContrast-1.png"),
            new TutorialStep("9-1) 폰트 변경", "[폰트 변경] 버튼을 누르면, 글씨체를 내 눈에 편한 걸로 바꿀 수 있어요.", "/Assets/Font.png"),
            new TutorialStep("9-2) 폰트 변경", "[폰트 변경] 버튼을 누르면 보이는 화면이에요. 원하는 글꼴을 고르고 적용시켜보세요.", "/Assets/Font-1.png"),
            new TutorialStep("10) 글자 크기 조절", "글씨가 작게 보여서 읽기 어려울 때는 [글자 크기 키우기(▲)], [글자 크기 줄이기(▼)] 버튼을 눌러서 조절해보세요.", "/Assets/FontSize.png"),
            new TutorialStep("11) 확대 / 축소", "웹페이지 전체를 확대, 축소할 수 있는 버튼이에요. \n원하는 크기로 화면을 조절할 수 있어요.", "/Assets/ZoomInOut.png"),
            new TutorialStep("12) 도움말", "지금까지의 설명을 또 보고싶으신가요??\n그렇다면 [도움말] 버튼을 눌러보세요. 프로그램에 대한 설명을 다시 볼 수 있어요.", "/Assets/help.png"),

        };


        public TutorialStepsWindow()
        {
            InitializeComponent();
            LoadStep(currentStep);
        }

    

        public TutorialStepsWindow(bool fromHelp = false)
        {
            InitializeComponent();
            isFromHelpButton = fromHelp;
            LoadStep(currentStep);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void LoadStep(int index)
        {
            if (index < 0 || index >= steps.Count) return;

            StepTitle.Text = steps[index].Title;
            StepDescription.Text = steps[index].Description;

            string imagePath = $"pack://application:,,,{steps[index].ImagePath}";
            StepImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));

            bool isLastStep = index == steps.Count - 1;

            if (isFromHelpButton)
            {
                // 도움말 경로
                FinishButton.Visibility = Visibility.Collapsed;
                StartButton.Visibility = Visibility.Collapsed;
                DontShowAgainCheck.Visibility = Visibility.Collapsed;
                CloseButton.Visibility = isLastStep ? Visibility.Visible : Visibility.Collapsed;

                PrevButton.Visibility = Visibility.Visible;
                NextButton.Visibility = isLastStep ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                // 일반 실행 경로
                FinishButton.Visibility = isLastStep ? Visibility.Visible : Visibility.Collapsed;
                StartButton.Visibility = isLastStep ? Visibility.Collapsed : Visibility.Visible;
                DontShowAgainCheck.Visibility = isLastStep ? Visibility.Visible : Visibility.Collapsed;
                CloseButton.Visibility = Visibility.Collapsed;

                PrevButton.Visibility = isLastStep ? Visibility.Collapsed : Visibility.Visible;
                NextButton.Visibility = isLastStep ? Visibility.Collapsed : Visibility.Visible;
            }
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
            // 도중에 바로 시작하기 누를 경우 → "다음부터 안 보기"로 저장
            Properties.Settings.Default.SkipTutorial = true;
            Properties.Settings.Default.Save();

            var main = new MainWindow();
            main.Show();
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

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            skipTutorialOnNextLaunch = DontShowAgainCheck.IsChecked == true;
            Properties.Settings.Default.SkipTutorial = skipTutorialOnNextLaunch;
            Properties.Settings.Default.Save();

            var main = new MainWindow();
            main.Show();
            this.Close();
        }


    }
}
