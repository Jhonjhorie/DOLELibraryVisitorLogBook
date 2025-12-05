using DoleVisitorLogbook.Database;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace DoleVisitorLogbook.Views
{
    public partial class AddEditVisitorWindow : Window
    {
        private bool isEditMode = false;
        private int visitorId = -1;

        // Constructor for Add Mode
        public AddEditVisitorWindow()
        {
            InitializeComponent();
            isEditMode = false;
            txtWindowTitle.Text = "Add New Visitor";
            txtHeaderIcon.Text = "➕";
            btnSave.Content = "💾 Save Visitor";

            // Set default values for Time In
            dpDateIn.SelectedDate = DateTime.Now.Date;
            txtTimeIn.Text = DateTime.Now.ToString("hh:mm tt");
        }

        // Constructor for Edit Mode
        public AddEditVisitorWindow(DataRowView visitorData)
        {
            InitializeComponent();
            isEditMode = true;
            txtWindowTitle.Text = "Edit Visitor";
            txtHeaderIcon.Text = "✏️";
            btnSave.Content = "💾 Update Visitor";

            LoadVisitorData(visitorData);
        }

        #region Load Data for Edit Mode

        private void LoadVisitorData(DataRowView visitorData)
        {
            try
            {
                visitorId = Convert.ToInt32(visitorData["id"]);
                txtName.Text = visitorData["name"].ToString();
                txtOffice.Text = visitorData["office"].ToString();
                txtPurpose.Text = visitorData["purpose"].ToString();

                // Set Gender
                string gender = visitorData["gender"].ToString();
                foreach (ComboBoxItem item in cbGender.Items)
                {
                    if (item.Content.ToString() == gender)
                    {
                        cbGender.SelectedItem = item;
                        break;
                    }
                }

                // Set Client Type
                string clientType = visitorData["client_type"].ToString();
                foreach (ComboBoxItem item in cbClientType.Items)
                {
                    if (item.Content.ToString() == clientType)
                    {
                        cbClientType.SelectedItem = item;
                        break;
                    }
                }

                // Set Time In
                if (visitorData["time_in"] != DBNull.Value)
                {
                    DateTime timeIn = Convert.ToDateTime(visitorData["time_in"]);
                    dpDateIn.SelectedDate = timeIn.Date;
                    txtTimeIn.Text = timeIn.ToString("hh:mm tt");
                }

                // Set Time Out (if exists)
                if (visitorData["time_out"] != DBNull.Value)
                {
                    DateTime timeOut = Convert.ToDateTime(visitorData["time_out"]);
                    dpDateOut.SelectedDate = timeOut.Date;
                    txtTimeOut.Text = timeOut.ToString("hh:mm tt");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading visitor data: {ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Validation

        private bool ValidateForm()
        {
            bool isValid = true;

            // Clear all error messages
            txtNameError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtClientTypeError.Visibility = Visibility.Collapsed;
            txtOfficeError.Visibility = Visibility.Collapsed;
            txtPurposeError.Visibility = Visibility.Collapsed;
            txtDateInError.Visibility = Visibility.Collapsed;
            txtTimeInError.Visibility = Visibility.Collapsed;
            txtTimeOutError.Visibility = Visibility.Collapsed;

            // Validate Name
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                txtNameError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Gender
            if (cbGender.SelectedItem == null)
            {
                txtGenderError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Client Type
            if (cbClientType.SelectedItem == null)
            {
                txtClientTypeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Office
            if (string.IsNullOrWhiteSpace(txtOffice.Text))
            {
                txtOfficeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Purpose
            if (string.IsNullOrWhiteSpace(txtPurpose.Text))
            {
                txtPurposeError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Date In
            if (!dpDateIn.SelectedDate.HasValue)
            {
                txtDateInError.Visibility = Visibility.Visible;
                isValid = false;
            }

            // Validate Time In
            if (string.IsNullOrWhiteSpace(txtTimeIn.Text))
            {
                txtTimeInError.Visibility = Visibility.Visible;
                txtTimeInError.Text = "Time In is required";
                isValid = false;
            }
            else if (!IsValidTime(txtTimeIn.Text))
            {
                txtTimeInError.Visibility = Visibility.Visible;
                txtTimeInError.Text = "Invalid time format (use 12:30 PM or 14:30)";
                isValid = false;
            }

            // Validate Time Out (if provided)
            if (!string.IsNullOrWhiteSpace(txtTimeOut.Text))
            {
                if (!IsValidTime(txtTimeOut.Text))
                {
                    txtTimeOutError.Visibility = Visibility.Visible;
                    txtTimeOutError.Text = "Invalid time format (use 12:30 PM or 14:30)";
                    isValid = false;
                }
            }

            return isValid;
        }

        private bool IsValidTime(string timeString)
        {
            // Try parsing 12-hour format with AM/PM
            string[] formats12Hour = new string[]
            {
                "h:mm tt", "hh:mm tt",
                "h:mmtt", "hh:mmtt",
                "h:mm:ss tt", "hh:mm:ss tt",
                "h:mm:sstt", "hh:mm:sstt"
            };

            if (DateTime.TryParseExact(timeString, formats12Hour,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            {
                return true;
            }

            // Try parsing 24-hour format
            if (TimeSpan.TryParse(timeString, out _))
            {
                return true;
            }

            return false;
        }

        private DateTime? ParseDateTime(DatePicker datePicker, TextBox timeTextBox)
        {
            if (!datePicker.SelectedDate.HasValue || string.IsNullOrWhiteSpace(timeTextBox.Text))
                return null;

            DateTime date = datePicker.SelectedDate.Value;
            string timeString = timeTextBox.Text.Trim();

            // Try parsing 12-hour format
            string[] formats12Hour = new string[]
            {
                "h:mm tt", "hh:mm tt",
                "h:mmtt", "hh:mmtt",
                "h:mm:ss tt", "hh:mm:ss tt",
                "h:mm:sstt", "hh:mm:sstt"
            };

            if (DateTime.TryParseExact(timeString, formats12Hour,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsed12Hour))
            {
                return date.Add(parsed12Hour.TimeOfDay);
            }

            // Try parsing 24-hour format
            if (TimeSpan.TryParse(timeString, out TimeSpan timeSpan))
            {
                return date.Add(timeSpan);
            }

            return null;
        }

        #endregion

        #region Save/Update

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateForm())
            {
                MessageBox.Show("Please fill in all required fields correctly.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnSave.IsEnabled = false;

                DateTime? timeIn = ParseDateTime(dpDateIn, txtTimeIn);
                DateTime? timeOut = null;

                if (dpDateOut.SelectedDate.HasValue && !string.IsNullOrWhiteSpace(txtTimeOut.Text))
                {
                    timeOut = ParseDateTime(dpDateOut, txtTimeOut);

                    // Validate Time Out is after Time In
                    if (timeOut.HasValue && timeIn.HasValue && timeOut.Value <= timeIn.Value)
                    {
                        MessageBox.Show("Time Out must be after Time In.",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        btnSave.IsEnabled = true;
                        return;
                    }
                }

                using (var conn = DB.GetConnection())
                {
                    conn.Open();
                    string sql;

                    if (isEditMode)
                    {
                        sql = @"UPDATE visitors 
                                SET name = @Name, 
                                    gender = @Gender, 
                                    client_type = @ClientType, 
                                    office = @Office, 
                                    purpose = @Purpose, 
                                    time_in = @TimeIn, 
                                    time_out = @TimeOut 
                                WHERE id = @Id";
                    }
                    else
                    {
                        sql = @"INSERT INTO visitors 
                                (name, gender, client_type, office, purpose, time_in, time_out) 
                                VALUES 
                                (@Name, @Gender, @ClientType, @Office, @Purpose, @TimeIn, @TimeOut)";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        if (isEditMode)
                        {
                            cmd.Parameters.AddWithValue("@Id", visitorId);
                        }

                        cmd.Parameters.AddWithValue("@Name", txtName.Text.Trim());
                        cmd.Parameters.AddWithValue("@Gender", ((ComboBoxItem)cbGender.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@ClientType", ((ComboBoxItem)cbClientType.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@Office", txtOffice.Text.Trim());
                        cmd.Parameters.AddWithValue("@Purpose", txtPurpose.Text.Trim());
                        cmd.Parameters.AddWithValue("@TimeIn", timeIn.Value.ToString("yyyy-MM-dd HH:mm:ss"));
                        cmd.Parameters.AddWithValue("@TimeOut", timeOut.HasValue ? (object)timeOut.Value.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);

                        cmd.ExecuteNonQuery();
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving visitor: {ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                btnSave.IsEnabled = true;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to cancel?\nAny unsaved changes will be lost.",
                "Confirm Cancel",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        #endregion
    }
}