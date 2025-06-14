using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WebSurfingHelpersForSenior
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message, string title = "알림", MessageBoxImage icon = MessageBoxImage.None)
        {
            InitializeComponent();
            MessageText.Text = message;

            switch (icon)
            {
                case MessageBoxImage.Information:
                    IconImage.Source = SystemIconsToImage(SystemIcons.Information);
                    break;
                case MessageBoxImage.Warning:
                    IconImage.Source = SystemIconsToImage(SystemIcons.Warning);
                    break;
                case MessageBoxImage.Error:
                    IconImage.Source = SystemIconsToImage(SystemIcons.Error);
                    break;
                case MessageBoxImage.Question:
                    IconImage.Source = SystemIconsToImage(SystemIcons.Question);
                    break;
                default:
                    IconImage.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public static void Show(string message, Window owner = null)
        {
            var box = new CustomMessageBox(message);
            if (owner != null) box.Owner = owner;
            box.ShowDialog();
        }

        public static void Show(string message, string title, MessageBoxImage icon, Window owner = null)
        {
            var box = new CustomMessageBox(message, title, icon);
            if (owner != null) box.Owner = owner;
            box.ShowDialog();
        }

        private BitmapImage SystemIconsToImage(System.Drawing.Icon sysIcon)
        {
            using (var iconStream = new System.IO.MemoryStream())
            {
                sysIcon.Save(iconStream);
                iconStream.Seek(0, System.IO.SeekOrigin.Begin);

                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = iconStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                return bitmap;
            }
        }
    }
}
