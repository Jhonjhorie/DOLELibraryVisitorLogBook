using DoleVisitorLogbook.Views;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DoleVisitorLogbook
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer? clockTimer;
        private Button? currentActiveButton;

        public MainWindow()
        {
            InitializeComponent();
            InitializeClock();
            LoadDashboard();

            // Set initial user info (you can customize this based on your login system)
            txtCurrentUser.Text = "Administrator";
        }

        #region Clock Timer

        private void InitializeClock()
        {
            clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += Timer_Tick;
            clockTimer.Start();
            UpdateDateTime();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            lblDateTime.Text = DateTime.Now.ToString("MMM dd, yyyy • hh:mm:ss tt");
            lblDayOfWeek.Text = DateTime.Now.ToString("dddd");
        }

        #endregion

        #region Navigation

        private void LoadDashboard()
        {
            try
            {
                MainContent.Children.Clear();
                MainContent.Children.Add(new DashboardView());

                SetActivePage(btnDashboard, "Dashboard", "Overview of visitor activity and statistics");
                btnQuickCheckIn.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                ShowError("Error loading Dashboard", ex.Message);
            }
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            LoadDashboard();
        }

        private void BtnVisitor_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = VisitorLogbookWindow.GetInstance();

                if (win.WindowState == WindowState.Minimized)
                    win.WindowState = WindowState.Normal;

                win.WindowState = WindowState.Maximized;
                win.Show();
                win.Activate();
                win.Focus();
                win.Topmost = true;
                win.Topmost = false;

                SetActivePage(btnVisitorLogbook, "Visitor Logbook", "Customer-facing check-in interface");
                btnQuickCheckIn.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError("Error opening Visitor Logbook", ex.Message);
            }
        }

        private void BtnManageLogbook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainContent.Children.Clear();
                MainContent.Children.Add(new ManageLogbook());

                SetActivePage(btnManageLogbook, "Manage Records", "View, edit, and manage visitor records");
                btnQuickCheckIn.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError("Error loading Manage Logbook", ex.Message);
            }
        }

        private void BtnReports_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainContent.Children.Clear();
                MainContent.Children.Add(new Reports());

                SetActivePage(btnReports, "Reports & Analytics", "Generate and view visitor reports");
                btnQuickCheckIn.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError("Error loading Reports", ex.Message);
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainContent.Children.Clear();
                MainContent.Children.Add(new Settings());

                SetActivePage(btnSettings, "Settings", "Configure application preferences");
                btnQuickCheckIn.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError("Error loading Settings", ex.Message);
            }
        }

        private void BtnQrcode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainContent.Children.Clear();
                MainContent.Children.Add(new QRCodeGeneratorControl());

                SetActivePage(btnQrCode, "QR Code Generator", "Generate QR codes for quick access");
                btnQuickCheckIn.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError("Error loading QR Code Generator", ex.Message);
            }
        }

        #endregion

        #region Helper Methods

        private void SetActivePage(Button activeButton, string pageTitle, string pageDescription)
        {
            // Remove active state from previous button
            if (currentActiveButton != null)
            {
                currentActiveButton.Tag = null;
            }

            // Set active state to new button
            activeButton.Tag = "Active";
            currentActiveButton = activeButton;

            // Update page title and description
            txtPageTitle.Text = pageTitle;
            txtPageDescription.Text = pageDescription;
        }

        private void ShowError(string title, string message)
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion

        #region Quick Actions

        private void BtnQuickCheckIn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addWindow = new AddEditVisitorWindow();
                if (addWindow.ShowDialog() == true)
                {
                    MessageBox.Show("Visitor checked in successfully!",
                        "Quick Check-In",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Refresh dashboard if it's currently loaded
                    if (currentActiveButton == btnDashboard)
                    {
                        LoadDashboard();
                    }
                }
            }
            catch (Exception ex)
            {
                ShowError("Error with Quick Check-In", ex.Message);
            }
        }

        #endregion

        #region Logout

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to logout?\n\nAll open windows will be closed.",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Stop the timer
                clockTimer?.Stop();

                // Close the Visitor Logbook window if open
                var visitorWindow = VisitorLogbookWindow.GetInstance();
                if (visitorWindow != null && visitorWindow.IsLoaded)
                {
                    visitorWindow.Close();
                }

                // You can add login window here instead of shutdown
                // var loginWindow = new LoginWindow();
                // loginWindow.Show();
                // this.Close();

                Application.Current.Shutdown();
            }
        }

        #endregion

        #region Window Events

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            clockTimer?.Stop();

            // Close the Visitor Logbook window if open
            var visitorWindow = VisitorLogbookWindow.GetInstance();
            if (visitorWindow != null && visitorWindow.IsLoaded)
            {
                visitorWindow.Close();
            }
        }

        #endregion

        #region Public Methods (for external access if needed)

        /// <summary>
        /// Navigate to a specific page programmatically
        /// </summary>
        public void NavigateToPage(string pageName)
        {
            switch (pageName.ToLower())
            {
                case "dashboard":
                    BtnDashboard_Click(btnDashboard, new RoutedEventArgs());
                    break;
                case "logbook":
                case "visitor":
                    BtnVisitor_Click(btnVisitorLogbook, new RoutedEventArgs());
                    break;
                case "manage":
                    BtnManageLogbook_Click(btnManageLogbook, new RoutedEventArgs());
                    break;
                case "reports":
                    BtnReports_Click(btnReports, new RoutedEventArgs());
                    break;
                case "settings":
                    BtnSettings_Click(btnSettings, new RoutedEventArgs());
                    break;
                case "qrcode":
                    BtnQrcode_Click(btnQrCode, new RoutedEventArgs());
                    break;
                default:
                    LoadDashboard();
                    break;
            }
        }

        /// <summary>
        /// Update the current user display
        /// </summary>
        public void SetCurrentUser(string username, string role = "Administrator")
        {
            txtCurrentUser.Text = username;
            // You can add role-based logic here
        }

        /// <summary>
        /// Show or hide the QR Code button
        /// </summary>
        public void ToggleQRCodeButton(bool show)
        {
            btnQrCode.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion
    }
}