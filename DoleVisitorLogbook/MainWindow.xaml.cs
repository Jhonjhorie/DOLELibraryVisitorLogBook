using DoleVisitorLogbook.Views;
using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DoleVisitorLogbook
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadDashboard();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
        private void Timer_Tick(object? sender, EventArgs e)
        {
            lblDateTime.Text = DateTime.Now.ToString("MMMM dd, yyyy HH:mm:ss");
        }

        private void LoadDashboard()
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new Views.DashboardView());
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e) => LoadDashboard();

        private void BtnAddVisitor_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new Views.AddVisitorView());
        }

        private void BtnVisitorLogbook_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new Views.VisitorLogbookView());
        }
        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new Views.Reports());
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {

            MainContent.Children.Clear();
            MainContent.Children.Add(new Views.Settings());
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

        private void BtnVisitor_Click(object sender, RoutedEventArgs e)
        {
            var win = VisitorLogbookWindow.GetInstance();

            if (win.WindowState == WindowState.Minimized)
                win.WindowState = WindowState.Normal;

            win.WindowState = WindowState.Maximized;
            win.Show();
            win.Activate();
            win.Focus();
            win.Topmost = true;   // bring to front
            win.Topmost = false;  // allow normal usage
        }

        private void BtnQrcode_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Children.Clear();
            MainContent.Children.Add(new Views.QRCodeGeneratorControl());
        }
    }
}