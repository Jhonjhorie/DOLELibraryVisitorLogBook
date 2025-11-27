using DoleVisitorLogbook.Database;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClosedXML.Excel;

namespace DoleVisitorLogbook.Views
{
    /// <summary>
    /// Interaction logic for Reports.xaml
    /// </summary>
    public partial class Reports : UserControl
    {
        public Reports()
        {
            InitializeComponent();
            LoadReports();
        }

        private void LoadReports()
        {
            using (var conn = DB.GetConnection())
            {
                conn.Open();
                string sql = "SELECT * FROM visitors ORDER BY time_in DESC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                ReportsDataGrid.ItemsSource = dt.DefaultView;
            }
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var dt = ((DataView)ReportsDataGrid.ItemsSource).ToTable();
            using (var wb = new XLWorkbook())
            {
                wb.Worksheets.Add(dt, "Visitors");
                wb.SaveAs("Visitors_Report.xlsx");
                MessageBox.Show("Exported successfully!");
            }
        }


    }
}
