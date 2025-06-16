using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WebSurfingHelpersForSenior.Properties;

namespace WebSurfingHelpersForSenior
{
    /// <summary>
    /// App.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            /*Settings.Default.SkipTutorial = false;
            Settings.Default.Save();*/

            base.OnStartup(e);

            if (Settings.Default.SkipTutorial)
            {
                var main = new MainWindow();
                main.Show();
            }
            else
            {
                var tutorial = new TutorialWindow();
                tutorial.Show(); 
            }
        }
    }
}
