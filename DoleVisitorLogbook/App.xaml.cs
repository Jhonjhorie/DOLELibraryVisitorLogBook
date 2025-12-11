using System.Configuration;
using System.Data;
using System.Windows;

namespace DoleVisitorLogbook
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Start with Login Window instead of MainWindow
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}