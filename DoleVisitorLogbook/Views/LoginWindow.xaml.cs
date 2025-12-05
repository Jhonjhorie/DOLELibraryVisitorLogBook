using DoleVisitorLogbook.Database;
using MySql.Data.MySqlClient;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace DoleVisitorLogbook
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtUsername.Focus();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            PerformLogin();
        }

        private void Input_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PerformLogin();
            }
        }

        private void PerformLogin()
        {
            // Clear previous error
            txtError.Visibility = Visibility.Collapsed;
            txtError.Text = "";

            // Validate input
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Please enter your username.");
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Please enter your password.");
                txtPassword.Focus();
                return;
            }

            // Disable login button during authentication
            btnLogin.IsEnabled = false;
            btnLogin.Content = "🔄 Logging in...";

            try
            {
                // Authenticate user
                if (AuthenticateUser(username, password))
                {
                    // Login successful
                    var mainWindow = new MainWindow();
                    mainWindow.Show();
                    this.Close();
                }
                else
                {
                    // Login failed
                    ShowError("Invalid username or password.");
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Login error: {ex.Message}");
            }
            finally
            {
                btnLogin.IsEnabled = true;
                btnLogin.Content = "🔓 LOGIN";
            }
        }

        private bool AuthenticateUser(string username, string password)
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT id, username, full_name, role, password FROM users WHERE username = @Username";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedPassword = reader["password"].ToString();
                                string hashedInputPassword = HashPassword(password);

                                // Verify password
                                if (storedPassword == hashedInputPassword)
                                {
                                    // Store user session
                                    UserSession.UserId = Convert.ToInt32(reader["id"]);
                                    UserSession.Username = reader["username"].ToString();
                                    UserSession.FullName = reader["full_name"].ToString();
                                    UserSession.Role = reader["role"].ToString();
                                    UserSession.LoginTime = DateTime.Now;

                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Database authentication error: {ex.Message}");
            }

            return false;
        }

        private string HashPassword(string password)
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

        private void ShowError(string message)
        {
            txtError.Text = message;
            txtError.Visibility = Visibility.Visible;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

    /// <summary>
    /// Static class to store current user session information
    /// </summary>
    public static class UserSession
    {
        public static int UserId { get; set; }
        public static string Username { get; set; } = string.Empty;
        public static string FullName { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
        public static DateTime LoginTime { get; set; }

        /// <summary>
        /// Check if user is logged in
        /// </summary>
        public static bool IsLoggedIn => UserId > 0;

        /// <summary>
        /// Check if user is an admin
        /// </summary>
        public static bool IsAdmin => Role.Equals("Admin", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Clear session (logout)
        /// </summary>
        public static void Clear()
        {
            UserId = 0;
            Username = string.Empty;
            FullName = string.Empty;
            Role = string.Empty;
            LoginTime = DateTime.MinValue;
        }
    }
}