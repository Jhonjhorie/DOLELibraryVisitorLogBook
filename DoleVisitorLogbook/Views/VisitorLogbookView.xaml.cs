using DoleVisitorLogbook.Database;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;

namespace DoleVisitorLogbook.Views
{
    /// <summary>
    /// Interaction logic for VisitorLogbookView.xaml
    /// </summary>
    public partial class VisitorLogbookView : UserControl
    {
        public VisitorLogbookView()
        {
            InitializeComponent();
            LoadVisitors();
        }

        private void LoadVisitors()
        {
            using (var conn = DB.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM visitors ORDER BY time_in DESC"; // <-- updated
                MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                dgVisitors.ItemsSource = dt.DefaultView;
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            var dt = ((DataView)dgVisitors.ItemsSource).ToTable();
            using (XLWorkbook wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt, "Visitors");
                wb.SaveAs("Visitors.xlsx");
                MessageBox.Show("Exported successfully!");
            }
        }
        private void CheckOut_Click(object sender, RoutedEventArgs e)
        {
            // Get the selected row
            DataRowView row = (DataRowView)((FrameworkElement)sender).DataContext;

            // Convert time_in to DateTime
            DateTime timeIn = Convert.ToDateTime(row["time_in"]);

            using (var conn = DB.GetConnection())
            {
                conn.Open();

                string sql = @"UPDATE visitors 
                       SET time_out = NOW() 
                       WHERE name = @Name AND time_in = @TimeIn AND time_out IS NULL";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Name", row["name"].ToString());
                    cmd.Parameters.AddWithValue("@TimeIn", timeIn); // pass DateTime directly

                    int updated = cmd.ExecuteNonQuery();

                    if (updated > 0)
                        MessageBox.Show("Visitor successfully checked out.");
                    else
                        MessageBox.Show("This visitor is already checked out.");
                }
            }

            // Refresh DataGrid
            LoadVisitors();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadVisitors();
        }

    }
}
