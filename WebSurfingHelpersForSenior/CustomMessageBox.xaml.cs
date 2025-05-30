using System.Windows;

namespace WebSurfingHelpersForSenior
{
    public partial class CustomMessageBox : Window
    {
        public CustomMessageBox(string message)
        {
            InitializeComponent();
            MessageText.Text = message;
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
    }
}
