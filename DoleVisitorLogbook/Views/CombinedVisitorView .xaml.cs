using DoleVisitorLogbook.Database;
using DoleVisitorLogbook.Model;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ClosedXML.Excel;

namespace DoleVisitorLogbook.Views
{
    public partial class VisitorLogbookWindow : Window
    {
        private static VisitorLogbookWindow? _instance;
        private DispatcherTimer? clockTimer;

        public static VisitorLogbookWindow GetInstance()
        {
            if (_instance == null)
                _instance = new VisitorLogbookWindow();

            return _instance;
        }

        public VisitorLogbookWindow()
        {
            InitializeComponent();
            WindowState = WindowState.Maximized;
            this.Closed += (s, e) => _instance = null;
            LoadVisitors();
            InitializeClock();
        }

        #region Clock Timer

        private void InitializeClock()
        {
            clockTimer = new DispatcherTimer();
            clockTimer.Interval = TimeSpan.FromSeconds(1);
            clockTimer.Tick += ClockTimer_Tick;
            clockTimer.Start();

            UpdateDateTime();
        }

        private void ClockTimer_Tick(object? sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            txtCurrentTime.Text = DateTime.Now.ToString("hh:mm:ss tt");
            txtCurrentDate.Text = DateTime.Now.ToString("MMMM dd, yyyy");
        }

        #endregion

        #region Visitor Logbook Methods

        private void LoadVisitors()
        {
            using (var conn = DB.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM visitors ORDER BY time_in DESC";
                MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgVisitors.ItemsSource = dt.DefaultView;
                txtRecordCount.Text = $"{dt.Rows.Count} records";
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadVisitors();
            MessageBox.Show("Visitor logbook refreshed!",
                "Refresh",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            Button btn = (Button)sender;
            var row = (DataRowView)btn.DataContext;
            string visitorName = row["name"].ToString();
            DateTime timeIn = Convert.ToDateTime(row["time_in"]);

            var container = dgVisitors.ItemContainerGenerator.ContainerFromItem(row) as DataGridRow;
            TextBox manualTxt = FindChild<TextBox>(container, "txtManualCheckout");

            string manualTimeValue = manualTxt?.Text.Trim();
            DateTime? manualCheckoutTime = null;

            if (!string.IsNullOrEmpty(manualTimeValue))
            {
                if (DateTime.TryParse(manualTimeValue, out DateTime parsedTime))
                {
                    manualCheckoutTime = parsedTime;
                }
                else
                {
                    MessageBox.Show("Invalid date format.\nUse: YYYY-MM-DD HH:MM:SS",
                        "Invalid Input",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            using (var conn = DB.GetConnection())
            {
                conn.Open();

                string sql = @"
            UPDATE visitors 
            SET time_out = @TimeOut 
            WHERE name = @Name AND time_in = @TimeIn AND time_out IS NULL";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", visitorName);
                    cmd.Parameters.AddWithValue("@TimeIn", timeIn);

                    cmd.Parameters.AddWithValue("@TimeOut",
                        manualCheckoutTime?.ToString("yyyy-MM-dd HH:mm:ss") ??
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    int updated = cmd.ExecuteNonQuery();

                    if (updated > 0)
                    {
                        MessageBox.Show($"Visitor '{visitorName}' successfully checked out.",
                            "Checkout Successful",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("This visitor is already checked out.",
                            "Already Checked Out",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }
                }
            }

            LoadVisitors();
        }

        #endregion

        #region Add Visitor Methods

        private bool ValidateForm()
        {
            bool isValid = true;

            txtNameError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtClientTypeError.Visibility = Visibility.Collapsed;
            txtOfficeError.Visibility = Visibility.Collapsed;
            txtPurposeError.Visibility = Visibility.Collapsed;

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                txtNameError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (cbGender.SelectedItem == null)
            {
                txtGenderError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (cbClientType.SelectedItem == null)
            {
                txtClientTypeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtOffice.Text))
            {
                txtOfficeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(txtPurpose.Text))
            {
                txtPurposeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            return isValid;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Please fill in all required fields marked with *",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnSave.IsEnabled = false;

                var visitor = new Visitor
                {
                    Name = txtName.Text.Trim(),
                    Gender = ((ComboBoxItem)cbGender.SelectedItem).Content.ToString(),
                    ClientType = ((ComboBoxItem)cbClientType.SelectedItem).Content.ToString(),
                    Office = txtOffice.Text.Trim(),
                    Purpose = txtPurpose.Text.Trim(),
                    TimeIn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                SaveVisitor(visitor);

                MessageBox.Show($"Visitor '{visitor.Name}' has been successfully checked in!\n\n" +
                               $"Time: {DateTime.Now.ToString("hh:mm tt")}\n" +
                               $"Thank you for visiting DOLE Library.",
                               "Check-In Successful",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                ClearForm();
                LoadVisitors();
                btnSave.IsEnabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred while saving the visitor:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                btnSave.IsEnabled = true;
            }
        }

        private void SaveVisitor(Visitor visitor)
        {
            using (var conn = DB.GetConnection())
            {
                conn.Open();

                string sql = @"INSERT INTO visitors
                       (name, gender, client_type, office, purpose, time_in)
                       VALUES (@Name, @Gender, @ClientType, @Office, @Purpose, @TimeIn)";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Name", visitor.Name);
                cmd.Parameters.AddWithValue("@Gender", visitor.Gender ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@ClientType", visitor.ClientType ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Office", visitor.Office ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Purpose", visitor.Purpose ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@TimeIn", visitor.TimeIn);

                cmd.ExecuteNonQuery();
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to clear all fields?",
                "Clear Form",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ClearForm();
            }
        }

        private void ClearForm()
        {
            txtName.Clear();
            txtOffice.Clear();
            txtPurpose.Clear();
            cbGender.SelectedIndex = -1;
            cbClientType.SelectedIndex = -1;

            txtNameError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtClientTypeError.Visibility = Visibility.Collapsed;
            txtOfficeError.Visibility = Visibility.Collapsed;
            txtPurposeError.Visibility = Visibility.Collapsed;

            txtName.Focus();
        }

        private static T? FindChild<T>(DependencyObject? parent, string childName) where T : DependencyObject
        {
            if (parent == null) return null;

            int childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is T childType)
                {
                    if (child is FrameworkElement frameworkElement &&
                        frameworkElement.Name == childName)
                        return childType;
                }

                var foundChild = FindChild<T>(child, childName);
                if (foundChild != null)
                    return foundChild;
            }
            return null;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            clockTimer?.Stop();
        }
    }
}
#endregion
