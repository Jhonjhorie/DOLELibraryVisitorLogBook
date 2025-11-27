using DoleVisitorLogbook.Database;
using DoleVisitorLogbook.Model;
using MySql.Data.MySqlClient;
using System;
using System.Windows;
using System.Windows.Controls;

namespace DoleVisitorLogbook.Views
{
    /// <summary>
    /// Interaction logic for AddVisitorView.xaml
    /// </summary>
    public partial class AddVisitorView : UserControl
    {
        public AddVisitorView()
        {
            InitializeComponent();
        }

        // Validate form inputs
        private bool ValidateForm()
        {
            bool isValid = true;

            // Reset all error messages
            txtNameError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtClientTypeError.Visibility = Visibility.Collapsed;
            txtOfficeError.Visibility = Visibility.Collapsed;
            txtPurposeError.Visibility = Visibility.Collapsed;

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

            // Validate Office/Institution
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

            return isValid;
        }

        // Save button click handler
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validate form first
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
                // Disable button to prevent double submission
                btnSave.IsEnabled = false;

                // Create visitor object
                var visitor = new Visitor
                {
                    Name = txtName.Text.Trim(),
                    Gender = ((ComboBoxItem)cbGender.SelectedItem).Content.ToString(),
                    ClientType = ((ComboBoxItem)cbClientType.SelectedItem).Content.ToString(),
                    Office = txtOffice.Text.Trim(),
                    Purpose = txtPurpose.Text.Trim(),
                    TimeIn = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };

                // Save to database
                SaveVisitor(visitor);

                // Show success message
                MessageBox.Show($"Visitor '{visitor.Name}' has been successfully checked in!\n\n" +
                               $"Time: {DateTime.Now.ToString("hh:mm tt")}\n" +
                               $"Thank you for visiting DOLE Library.",
                               "Check-In Successful",
                               MessageBoxButton.OK,
                               MessageBoxImage.Information);

                // Clear the form
                ClearForm();

                // Re-enable button
                btnSave.IsEnabled = true;
            }
            catch (MySqlException ex)
            {
                MessageBox.Show($"Database error: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

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

        // Save visitor to database
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

        // Clear form button click handler
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

        // Clear all form fields
        private void ClearForm()
        {
            // Clear text boxes
            txtName.Clear();
            txtOffice.Clear();
            txtPurpose.Clear();

            // Reset combo boxes
            cbGender.SelectedIndex = -1;
            cbClientType.SelectedIndex = -1;

            // Hide error messages
            txtNameError.Visibility = Visibility.Collapsed;
            txtGenderError.Visibility = Visibility.Collapsed;
            txtClientTypeError.Visibility = Visibility.Collapsed;
            txtOfficeError.Visibility = Visibility.Collapsed;
            txtPurposeError.Visibility = Visibility.Collapsed;

            // Focus on first field
            txtName.Focus();
        }
    }
}