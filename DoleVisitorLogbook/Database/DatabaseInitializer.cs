using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;

namespace DoleVisitorLogbook.Database
{
    public static class DatabaseInitializer
    {
        private const string SERVER_CONN =
            "Server=localhost;Port=3307;Uid=root;Pwd=Information@1;";

        private const string DB_CONN =
            "Server=localhost;Port=3307;Database=dole_logbook;Uid=root;Pwd=Information@1;";

        public static void Initialize()
        {
            if (!DatabaseExists())
            {
                CreateDatabase();
                CreateVisitorTable();
                CreateUserTable();
                EnsureAdminUser();
            }
        }

        private static void CreateDatabase()
        {
            using (var conn = new MySqlConnection(SERVER_CONN))
            {
                conn.Open();
                string sql = "CREATE DATABASE IF NOT EXISTS dole_logbook;";
                new MySqlCommand(sql, conn).ExecuteNonQuery();
            }
        }

        private static void CreateVisitorTable()
        {
            using (var conn = new MySqlConnection(DB_CONN))
            {
                conn.Open();

                string sql = @"
                CREATE TABLE IF NOT EXISTS visitors (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    name VARCHAR(255) NOT NULL,
                    gender VARCHAR(50),
                    client_type VARCHAR(50),
                    office VARCHAR(255),
                    purpose TEXT,
                    time_in DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    time_out DATETIME
                );";

                new MySqlCommand(sql, conn).ExecuteNonQuery();
            }
        }

        private static void CreateUserTable()
        {
            using (var conn = new MySqlConnection(DB_CONN))
            {
                conn.Open();

                string sql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    username VARCHAR(50) NOT NULL UNIQUE,
                    password VARCHAR(255) NOT NULL,
                    full_name VARCHAR(100) NOT NULL,
                    role VARCHAR(20) NOT NULL,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                );";

                new MySqlCommand(sql, conn).ExecuteNonQuery();
            }
        }


        private static void EnsureAdminUser()
        {
            using (var conn = new MySqlConnection(DB_CONN))
            {
                conn.Open();

                // Check if any user exists
                string checkSql = "SELECT COUNT(*) FROM users;";
                long userCount = (long)new MySqlCommand(checkSql, conn).ExecuteScalar();

                if (userCount == 0)
                {
                    string insertAdmin = @"
                        INSERT INTO users (username, password, full_name, role)
                        VALUES (@username, @password, @full_name, @role);";

                    using (var cmd = new MySqlCommand(insertAdmin, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", "admin");
                        cmd.Parameters.AddWithValue("@password", HashPassword("admin123"));
                        cmd.Parameters.AddWithValue("@full_name", "System Administrator");
                        cmd.Parameters.AddWithValue("@role", "Admin");

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }


        private static string HashPassword(string password)
            {
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
                    StringBuilder builder = new StringBuilder();
                    foreach (byte b in bytes)
                        builder.Append(b.ToString("x2"));
                    return builder.ToString();
                }
            }


        public static bool DatabaseExists()
        {
            using (var conn = new MySqlConnection(SERVER_CONN))
            {
                conn.Open();

                string sql = @"
                    SELECT SCHEMA_NAME
                    FROM INFORMATION_SCHEMA.SCHEMATA
                    WHERE SCHEMA_NAME = 'dole_logbook';";

                object result = new MySqlCommand(sql, conn).ExecuteScalar();
                return result != null;
            }
        }


    }
}
