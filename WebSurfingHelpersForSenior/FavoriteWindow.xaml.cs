using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WebSurfingHelpersForSenior.services;

namespace WebSurfingHelpersForSenior
{
    /// <summary>
    /// FavoriteWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FavoriteWindow : Window
    {
        public FavoriteWindow()
        {
            InitializeComponent();
            LoadFavorites();
        }

        // 즐겨찾기 목록을 화면에 로드하는 함수
        private void LoadFavorites()
        {
            if (Properties.Settings.Default.FavoritesList == null)
            {
                Properties.Settings.Default.FavoritesList = new System.Collections.Specialized.StringCollection();
            }

            FavoritesList.Items.Clear();
            foreach (var fav in FavoritesStorage.Load())
            {
                FavoritesList.Items.Add(fav);
            }
        }

        // 현재 페이지를 즐겨찾기에 추가하는 함수
        private void AddFavorite_Click(object sender, RoutedEventArgs e)
        {
            var main = this.Owner as MainWindow;
            var currentUrl = main?.CurrentUrl;

            if (!string.IsNullOrEmpty(currentUrl) )
            {
                // 별명 입력
                string name = Microsoft.VisualBasic.Interaction.InputBox(
                    "사이트 이름(별명)을 입력하세요: ", "별명 입력", "새 사이트");

                if (string.IsNullOrEmpty(name)) return;

                var list = FavoritesStorage.Load();
                if (!list.Any(f => f.Url == currentUrl))
                {
                    list.Add(new FavoriteItem { Name = name, Url = currentUrl });
                    FavoritesStorage.Save(list);
                    LoadFavorites();
                }
            }
        }

        // 선택된 즐겨찾기를 삭제하는 함수
        private void RemoveFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (FavoritesList.SelectedItem is FavoriteItem selected)
            {
                var list = FavoritesStorage.Load();
                var toRemove = list.FirstOrDefault(f => f.Url == selected.Url && f.Name == selected.Name);

                if (toRemove != null)
                {
                    list.Remove(toRemove);
                    FavoritesStorage.Save(list);
                    LoadFavorites();
                }
            }
        }

        // 선택한 즐겨찾기 URL로 이동하는 함수
        private void NavigateFavorite_Click(object sender, RoutedEventArgs e)
        {
            if (FavoritesList.SelectedItem is FavoriteItem selected)
            {
                (this.Owner as MainWindow)?.NavigateToUrl(selected.Url);
                this.Close();
            }
        }

        // 항목 선택 시 버튼 활성화 처리 (선택한 게 있을 때만 버튼 활성화)
        private void FavoritesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NavigateButton.IsEnabled = FavoritesList.SelectedItem != null;
        }
    }
}
