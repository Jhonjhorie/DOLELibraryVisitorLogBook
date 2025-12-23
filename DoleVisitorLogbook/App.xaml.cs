using System.Windows;
using DoleVisitorLogbook.Database;

namespace DoleVisitorLogbook
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // 🔹 Ensure database & tables exist
                DatabaseInitializer.Initialize();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(
                    "Database initialization failed.\n\n" +
                    "Please ensure MySQL Server is installed and running.\n\n" +
                    ex.Message,
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                Shutdown(); // stop app if DB is not ready
                return;
            }

            // 🔹 Start with Login Window
            var loginWindow = new LoginWindow();
            loginWindow.Show();
        }
    }
}
