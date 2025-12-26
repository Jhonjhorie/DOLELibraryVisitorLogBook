using DoleVisitorLogbook.Database;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace DoleVisitorLogbook.Views
{
    public partial class Settings : UserControl
    {
        private DataTable allUsersData = new DataTable();
        private int selectedUserId = -1;

        public Settings()
        {
            InitializeComponent();
            LoadAllUsers();
        }

        #region Load Data

        private void LoadAllUsers()
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT id, username, full_name, role, created_at, updated_at FROM users ORDER BY id ASC";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    allUsersData = new DataTable();
                    da.Fill(allUsersData);

                    // Add formatted columns
                    allUsersData.Columns.Add("created_at_formatted", typeof(string));
                    allUsersData.Columns.Add("updated_at_formatted", typeof(string));

                    foreach (DataRow row in allUsersData.Rows)
                    {
                        if (row["created_at"] != DBNull.Value)
                        {
                            DateTime createdAt = Convert.ToDateTime(row["created_at"]);
                            row["created_at_formatted"] = createdAt.ToString("MM/dd/yyyy hh:mm tt");
                        }

                        if (row["updated_at"] != DBNull.Value)
                        {
                            DateTime updatedAt = Convert.ToDateTime(row["updated_at"]);
                            row["updated_at_formatted"] = updatedAt.ToString("MM/dd/yyyy hh:mm tt");
                        }
                    }

                    dgUsers.ItemsSource = allUsersData.DefaultView;
                    txtTotalUsers.Text = $"{allUsersData.Rows.Count} Total Users";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading users: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Edit User Window

        public partial class EditUserWindow : Window
        {
            private int userId;
            private string currentUsername;

            public EditUserWindow(int userId, DataRowView userData)
            {
                this.userId = userId;
                this.currentUsername = userData["username"].ToString();
                InitializeComponent();
                LoadRoles();
                LoadUserData(userData);
            }

            private void InitializeComponent()
            {
                Width = 450;
                Height = 400;
                Title = "Edit User";
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                ResizeMode = ResizeMode.NoResize;

                var grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                // Header
                var headerText = new TextBlock
                {
                    Text = "✏️ Edit User Account",
                    FontSize = 20,
                    FontWeight = FontWeights.Bold,
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 81, 181)),
                    Margin = new Thickness(0, 0, 0, 20)
                };
                Grid.SetRow(headerText, 0);
                grid.Children.Add(headerText);

                // Form
                var formStack = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
                Grid.SetRow(formStack, 1);

                // Username
                formStack.Children.Add(CreateLabel("Username:"));
                txtUsername = CreateTextBox();
                formStack.Children.Add(txtUsername);

                // Full Name
                formStack.Children.Add(CreateLabel("Full Name:"));
                txtFullName = CreateTextBox();
                formStack.Children.Add(txtFullName);

                // Role
                formStack.Children.Add(CreateLabel("Role:"));
                cmbRole = new ComboBox
                {
                    Height = 35,
                    FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                formStack.Children.Add(cmbRole);

                grid.Children.Add(formStack);

                // Buttons
                var buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };
                Grid.SetRow(buttonPanel, 2);

                var btnSave = new Button
                {
                    Content = "💾 Update User",
                    Width = 120,
                    Height = 40,
                    Margin = new Thickness(0, 0, 10, 0),
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 107, 192)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnSave.Click += BtnSave_Click;

                var btnCancel = new Button
                {
                    Content = "Cancel",
                    Width = 100,
                    Height = 40,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)),
                    Foreground = System.Windows.Media.Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Cursor = System.Windows.Input.Cursors.Hand
                };
                btnCancel.Click += (s, e) => DialogResult = false;

                buttonPanel.Children.Add(btnSave);
                buttonPanel.Children.Add(btnCancel);
                grid.Children.Add(buttonPanel);

                Content = grid;
            }

            private TextBlock CreateLabel(string text)
            {
                return new TextBlock
                {
                    Text = text,
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 107, 192))
                };
            }

            private TextBox CreateTextBox()
            {
                return new TextBox
                {
                    Height = 35,
                    FontSize = 13,
                    Padding = new Thickness(10),
                    Margin = new Thickness(0, 0, 0, 15)
                };
            }

            private TextBox txtUsername;
            private TextBox txtFullName;
            private ComboBox cmbRole;

            private void LoadRoles()
            {
                cmbRole.Items.Add("Admin");
                cmbRole.Items.Add("User");
            }

            private void LoadUserData(DataRowView userData)
            {
                txtUsername.Text = userData["username"].ToString();
                txtFullName.Text = userData["full_name"].ToString();

                string role = userData["role"].ToString();
                cmbRole.SelectedIndex = role.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            }

            private void BtnSave_Click(object sender, RoutedEventArgs e)
            {
                // Validation
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtFullName.Text))
                {
                    MessageBox.Show("Please enter a full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                try
                {
                    using (var conn = DB.GetConnection())
                    {
                        conn.Open();

                        // Check if username already exists (excluding current user)
                        if (txtUsername.Text.Trim() != currentUsername)
                        {
                            string checkSql = "SELECT COUNT(*) FROM users WHERE username = @Username AND id != @Id";
                            using (MySqlCommand checkCmd = new MySqlCommand(checkSql, conn))
                            {
                                checkCmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                                checkCmd.Parameters.AddWithValue("@Id", userId);
                                int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                                if (count > 0)
                                {
                                    MessageBox.Show("Username already exists. Please choose a different username.",
                                        "Duplicate Username",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Warning);
                                    return;
                                }
                            }
                        }

                        // Update user
                        string sql = @"UPDATE users 
                                   SET username = @Username, 
                                       full_name = @FullName, 
                                       role = @Role, 
                                       updated_at = @UpdatedAt 
                                   WHERE id = @Id";

                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                            cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                            cmd.Parameters.AddWithValue("@Role", cmbRole.SelectedItem.ToString());
                            cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                            cmd.Parameters.AddWithValue("@Id", userId);

                            cmd.ExecuteNonQuery();
                            DialogResult = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error updating user: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Search

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            if (allUsersData == null) return;

            try
            {
                string filterExpression = "";

                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.Trim().Replace("'", "''");
                    filterExpression = $"username LIKE '%{searchText}%' OR full_name LIKE '%{searchText}%'";
                }

                allUsersData.DefaultView.RowFilter = filterExpression;
                txtTotalUsers.Text = $"{allUsersData.DefaultView.Count} Total Users";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying search filter: {ex.Message}",
                    "Filter Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void BtnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            allUsersData.DefaultView.RowFilter = "";
            txtTotalUsers.Text = $"{allUsersData.Rows.Count} Total Users";
        }

        #endregion

        #region User Management

        private void BtnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var addUserWindow = new AddUserWindow();
            if (addUserWindow.ShowDialog() == true)
            {
                LoadAllUsers();
                MessageBox.Show("User added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUserId == -1)
            {
                MessageBox.Show("Please select a user to edit.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var selectedRow = (DataRowView)dgUsers.SelectedItem;
            var editWindow = new EditUserWindow(selectedUserId, selectedRow);

            if (editWindow.ShowDialog() == true)
            {
                LoadAllUsers();
                MessageBox.Show("User updated successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUserId == -1)
            {
                MessageBox.Show("Please select a user to change password.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var selectedRow = (DataRowView)dgUsers.SelectedItem;
            string username = selectedRow["username"].ToString();

            var changePasswordWindow = new ChangePasswordWindow(selectedUserId, username);
            if (changePasswordWindow.ShowDialog() == true)
            {
                LoadAllUsers();
                MessageBox.Show("Password changed successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUserId == -1)
            {
                MessageBox.Show("Please select a user to delete.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var selectedRow = (DataRowView)dgUsers.SelectedItem;
            string username = selectedRow["username"].ToString();

            // Prevent deleting the current logged-in user
            // You might want to add logic to check if this is the current user

            var result = MessageBox.Show(
                $"Are you sure you want to delete user '{username}'?\n\nThis action cannot be undone.",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var conn = DB.GetConnection())
                    {
                        conn.Open();
                        string sql = "DELETE FROM users WHERE id = @Id";
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", selectedUserId);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("User deleted successfully!",
                                    "Deleted",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                                LoadAllUsers();
                                selectedUserId = -1;
                                btnChangePassword.IsEnabled = false;
                                btnDeleteUser.IsEnabled = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting user: {ex.Message}",
                        "Delete Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region UI Events

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAllUsers();
            selectedUserId = -1;
            btnChangePassword.IsEnabled = false;
            btnDeleteUser.IsEnabled = false;
            btnEditUser.IsEnabled = false;
        }

        private void BtnExportUsers_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (allUsersData == null || allUsersData.Rows.Count == 0)
                {
                    MessageBox.Show("No users to export.",
                        "Export Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Export Users List",
                    FileName = $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Users");

                        // Add headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Username";
                        worksheet.Cell(1, 3).Value = "Full Name";
                        worksheet.Cell(1, 4).Value = "Role";
                        worksheet.Cell(1, 5).Value = "Created Date";
                        worksheet.Cell(1, 6).Value = "Last Modified";

                        // Style headers
                        var headerRange = worksheet.Range(1, 1, 1, 6);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#5C6BC0");
                        headerRange.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
                        headerRange.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                        // Add data (exclude password column for security)
                        int row = 2;
                        foreach (DataRow dataRow in allUsersData.Rows)
                        {
                            worksheet.Cell(row, 1).Value = dataRow["id"].ToString();
                            worksheet.Cell(row, 2).Value = dataRow["username"].ToString();
                            worksheet.Cell(row, 3).Value = dataRow["full_name"].ToString();
                            worksheet.Cell(row, 4).Value = dataRow["role"].ToString();
                            worksheet.Cell(row, 5).Value = dataRow["created_at_formatted"].ToString();
                            worksheet.Cell(row, 6).Value = dataRow["updated_at_formatted"].ToString();
                            row++;
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();

                        // Add alternating row colors
                        for (int i = 2; i <= row - 1; i++)
                        {
                            if (i % 2 == 0)
                            {
                                worksheet.Range(i, 1, i, 6).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#F3E5F5");
                            }
                        }

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show($"Users exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting users: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgUsers.SelectedItem != null)
            {
                var selectedRow = (DataRowView)dgUsers.SelectedItem;
                selectedUserId = Convert.ToInt32(selectedRow["id"]);
                btnChangePassword.IsEnabled = true;
                btnDeleteUser.IsEnabled = true;
                btnEditUser.IsEnabled = true;
            }
            else
            {
                selectedUserId = -1;
                btnChangePassword.IsEnabled = false;
                btnDeleteUser.IsEnabled = false;
                btnEditUser.IsEnabled = false;
            }
        }

        #endregion

        #region Helper Methods

        public static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        #endregion
    }

    #region Add User Window

    public partial class AddUserWindow : Window
    {
        public AddUserWindow()
        {
            InitializeComponent();
            LoadRoles();
        }

        private void InitializeComponent()
        {
            Width = 450;
            Height = 530;
            Title = "Add New User";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerText = new TextBlock
            {
                Text = "➕ Create New User Account",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 81, 181)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(headerText, 0);
            grid.Children.Add(headerText);

            // Form
            var formStack = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            Grid.SetRow(formStack, 1);

            // Username
            formStack.Children.Add(CreateLabel("Username:"));
            txtUsername = CreateTextBox();
            formStack.Children.Add(txtUsername);

            // Full Name
            formStack.Children.Add(CreateLabel("Full Name:"));
            txtFullName = CreateTextBox();
            formStack.Children.Add(txtFullName);

            // Role
            formStack.Children.Add(CreateLabel("Role:"));
            cmbRole = new ComboBox
            {
                Height = 35,
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 15)
            };
            formStack.Children.Add(cmbRole);

            // Password
            formStack.Children.Add(CreateLabel("Password:"));
            txtPassword = new PasswordBox
            {
                Height = 35,
                FontSize = 13,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };
            formStack.Children.Add(txtPassword);

            // Confirm Password
            formStack.Children.Add(CreateLabel("Confirm Password:"));
            txtConfirmPassword = new PasswordBox
            {
                Height = 35,
                FontSize = 13,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            formStack.Children.Add(txtConfirmPassword);

            grid.Children.Add(formStack);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 2);

            var btnSave = new Button
            {
                Content = "💾 Save User",
                Width = 120,
                Height = 40,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 107, 192)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(btnSave);
            buttonPanel.Children.Add(btnCancel);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 107, 192))
            };
        }

        private TextBox CreateTextBox()
        {
            return new TextBox
            {
                Height = 35,
                FontSize = 13,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };
        }

        private TextBox txtUsername;
        private TextBox txtFullName;
        private ComboBox cmbRole;
        private PasswordBox txtPassword;
        private PasswordBox txtConfirmPassword;

        private void LoadRoles()
        {
            cmbRole.Items.Add("Admin");
            cmbRole.Items.Add("User");
            cmbRole.SelectedIndex = 1; // Default to User
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                MessageBox.Show("Please enter a username.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Please enter a full name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                MessageBox.Show("Please enter a password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtPassword.Password != txtConfirmPassword.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtPassword.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();

                    // Check if username already exists
                    string checkSql = "SELECT COUNT(*) FROM users WHERE username = @Username";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Username already exists. Please choose a different username.",
                                "Duplicate Username",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                    }

                    // Insert new user
                    string sql = @"INSERT INTO users (username, password, full_name, role, created_at, updated_at) 
                                   VALUES (@Username, @Password, @FullName, @Role, @CreatedAt, @UpdatedAt)";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", txtUsername.Text.Trim());
                        cmd.Parameters.AddWithValue("@Password", Settings.HashPassword(txtPassword.Password));
                        cmd.Parameters.AddWithValue("@FullName", txtFullName.Text.Trim());
                        cmd.Parameters.AddWithValue("@Role", cmbRole.SelectedItem.ToString());
                        cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);

                        cmd.ExecuteNonQuery();
                        DialogResult = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding user: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion

    #region Change Password Window

    public partial class ChangePasswordWindow : Window
    {
        private int userId;
        private string username;

        public ChangePasswordWindow(int userId, string username)
        {
            this.userId = userId;
            this.username = username;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Width = 450;
            Height = 350;
            Title = "Change Password";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerText = new TextBlock
            {
                Text = $"🔑 Change Password for: {username}",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 81, 181)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(headerText, 0);
            grid.Children.Add(headerText);

            // Form
            var formStack = new StackPanel { Margin = new Thickness(0, 0, 0, 20) };
            Grid.SetRow(formStack, 1);

            // New Password
            formStack.Children.Add(CreateLabel("New Password:"));
            txtNewPassword = new PasswordBox
            {
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };
            formStack.Children.Add(txtNewPassword);

            // Confirm Password
            formStack.Children.Add(CreateLabel("Confirm New Password:"));
            txtConfirmPassword = new PasswordBox
            {
                Height = 40,
                FontSize = 14,
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10)
            };
            formStack.Children.Add(txtConfirmPassword);

            grid.Children.Add(formStack);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(buttonPanel, 2);

            var btnSave = new Button
            {
                Content = "💾 Change Password",
                Width = 150,
                Height = 40,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Content = "Cancel",
                Width = 100,
                Height = 40,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(158, 158, 158)),
                Foreground = System.Windows.Media.Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand
            };
            btnCancel.Click += (s, e) => DialogResult = false;

            buttonPanel.Children.Add(btnSave);
            buttonPanel.Children.Add(btnCancel);
            grid.Children.Add(buttonPanel);

            Content = grid;
        }

        private TextBlock CreateLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(92, 107, 192))
            };
        }

        private PasswordBox txtNewPassword;
        private PasswordBox txtConfirmPassword;

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(txtNewPassword.Password))
            {
                MessageBox.Show("Please enter a new password.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtNewPassword.Password != txtConfirmPassword.Password)
            {
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (txtNewPassword.Password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();
                    string sql = "UPDATE users SET password = @Password, updated_at = @UpdatedAt WHERE id = @Id";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Password", Settings.HashPassword(txtNewPassword.Password));
                        cmd.Parameters.AddWithValue("@UpdatedAt", DateTime.Now);
                        cmd.Parameters.AddWithValue("@Id", userId);

                        cmd.ExecuteNonQuery();
                        DialogResult = true;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error changing password: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    #endregion
}