using DoleVisitorLogbook.Database;
using DoleVisitorLogbook.Model;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace DoleVisitorLogbook.Views
{
    public partial class ManageLogbook : UserControl
    {
        private DataTable allVisitorsData = new DataTable();
        private int selectedVisitorId = -1;

        public ManageLogbook()
        {
            InitializeComponent();
            LoadAllVisitors();
        }

        #region Load Data

        private void LoadAllVisitors()
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();
                    string sql = "SELECT * FROM visitors ORDER BY time_in DESC";
                    MySqlDataAdapter da = new MySqlDataAdapter(sql, conn);
                    allVisitorsData = new DataTable();
                    da.Fill(allVisitorsData);

                    // Add formatted columns
                    allVisitorsData.Columns.Add("date_formatted", typeof(string));
                    allVisitorsData.Columns.Add("time_in_formatted", typeof(string));
                    allVisitorsData.Columns.Add("time_out_formatted", typeof(string));

                    foreach (DataRow row in allVisitorsData.Rows)
                    {
                        if (row["time_in"] != DBNull.Value)
                        {
                            DateTime timeIn = Convert.ToDateTime(row["time_in"]);
                            row["date_formatted"] = timeIn.ToString("MM/dd/yyyy");
                            row["time_in_formatted"] = timeIn.ToString("hh:mm tt");
                        }

                        if (row["time_out"] != DBNull.Value)
                        {
                            DateTime timeOut = Convert.ToDateTime(row["time_out"]);
                            row["time_out_formatted"] = timeOut.ToString("hh:mm tt");
                        }
                        else
                        {
                            row["time_out_formatted"] = "";
                        }
                    }

                    dgAdminVisitors.ItemsSource = allVisitorsData.DefaultView;
                    txtTotalRecords.Text = $"{allVisitorsData.Rows.Count} Total Records";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading visitors: {ex.Message}",
                    "Database Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Search and Filter

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DateFilter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            if (allVisitorsData == null) return;

            try
            {
                string filterExpression = "";

                // Name search filter
                if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                {
                    string searchText = txtSearch.Text.Trim().Replace("'", "''");
                    filterExpression = $"name LIKE '%{searchText}%'";
                }

                // Date range filter
                if (dpDateFrom.SelectedDate.HasValue && dpDateTo.SelectedDate.HasValue)
                {
                    DateTime dateFrom = dpDateFrom.SelectedDate.Value.Date;
                    DateTime dateTo = dpDateTo.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

                    string dateFilter = $"time_in >= #{dateFrom:MM/dd/yyyy}# AND time_in <= #{dateTo:MM/dd/yyyy HH:mm:ss}#";

                    if (!string.IsNullOrEmpty(filterExpression))
                        filterExpression += $" AND {dateFilter}";
                    else
                        filterExpression = dateFilter;
                }
                else if (dpDateFrom.SelectedDate.HasValue)
                {
                    DateTime dateFrom = dpDateFrom.SelectedDate.Value.Date;
                    string dateFilter = $"time_in >= #{dateFrom:MM/dd/yyyy}#";

                    if (!string.IsNullOrEmpty(filterExpression))
                        filterExpression += $" AND {dateFilter}";
                    else
                        filterExpression = dateFilter;
                }
                else if (dpDateTo.SelectedDate.HasValue)
                {
                    DateTime dateTo = dpDateTo.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);
                    string dateFilter = $"time_in <= #{dateTo:MM/dd/yyyy HH:mm:ss}#";

                    if (!string.IsNullOrEmpty(filterExpression))
                        filterExpression += $" AND {dateFilter}";
                    else
                        filterExpression = dateFilter;
                }

                allVisitorsData.DefaultView.RowFilter = filterExpression;
                txtTotalRecords.Text = $"{allVisitorsData.DefaultView.Count} Total Records";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filters: {ex.Message}",
                    "Filter Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void BtnClearFilters_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Clear();
            dpDateFrom.SelectedDate = null;
            dpDateTo.SelectedDate = null;
            allVisitorsData.DefaultView.RowFilter = "";
            txtTotalRecords.Text = $"{allVisitorsData.Rows.Count} Total Records";
        }

        #endregion

        #region CRUD Operations

        private void BtnAddVisitor_Click(object sender, RoutedEventArgs e)
        {
            var addWindow = new AddEditVisitorWindow();
            if (addWindow.ShowDialog() == true)
            {
                LoadAllVisitors();
                MessageBox.Show("Visitor added successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedVisitorId == -1)
            {
                MessageBox.Show("Please select a visitor to edit.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var selectedRow = (DataRowView)dgAdminVisitors.SelectedItem;
            var editWindow = new AddEditVisitorWindow(selectedRow);

            if (editWindow.ShowDialog() == true)
            {
                LoadAllVisitors();
                MessageBox.Show("Visitor updated successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (selectedVisitorId == -1)
            {
                MessageBox.Show("Please select a visitor to delete.",
                    "No Selection",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Are you sure you want to delete this visitor record?\n\nThis action cannot be undone.",
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
                        string sql = "DELETE FROM visitors WHERE id = @Id";
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@Id", selectedVisitorId);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                MessageBox.Show("Visitor record deleted successfully!",
                                    "Deleted",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Information);
                                LoadAllVisitors();
                                selectedVisitorId = -1;
                                btnEdit.IsEnabled = false;
                                btnDelete.IsEnabled = false;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting visitor: {ex.Message}",
                        "Delete Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region Export
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the filtered view instead of all data
                var filteredView = allVisitorsData.DefaultView;

                if (filteredView == null || filteredView.Count == 0)
                {
                    MessageBox.Show("No data to export.",
                        "Export Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Export Visitor Logbook",
                    FileName = $"VisitorLogbook_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Visitor Logbook");

                        // Add headers
                        worksheet.Cell(1, 1).Value = "ID";
                        worksheet.Cell(1, 2).Value = "Date";
                        worksheet.Cell(1, 3).Value = "Name";
                        worksheet.Cell(1, 4).Value = "Gender";
                        worksheet.Cell(1, 5).Value = "Client Type";
                        worksheet.Cell(1, 6).Value = "Office/Institution";
                        worksheet.Cell(1, 7).Value = "Purpose";
                        worksheet.Cell(1, 8).Value = "Time In";
                        worksheet.Cell(1, 9).Value = "Time Out";

                        // Style headers
                        var headerRange = worksheet.Range(1, 1, 1, 9);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#5C6BC0");
                        headerRange.Style.Font.FontColor = XLColor.White;
                        headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // Add data from filtered view
                        int row = 2;
                        foreach (DataRowView dataRowView in filteredView)
                        {
                            DataRow dataRow = dataRowView.Row;

                            worksheet.Cell(row, 1).Value = dataRow["id"].ToString();
                            worksheet.Cell(row, 2).Value = dataRow["date_formatted"].ToString();
                            worksheet.Cell(row, 3).Value = dataRow["name"].ToString();
                            worksheet.Cell(row, 4).Value = dataRow["gender"].ToString();
                            worksheet.Cell(row, 5).Value = dataRow["client_type"].ToString();
                            worksheet.Cell(row, 6).Value = dataRow["office"].ToString();
                            worksheet.Cell(row, 7).Value = dataRow["purpose"].ToString();
                            worksheet.Cell(row, 8).Value = dataRow["time_in_formatted"].ToString();
                            worksheet.Cell(row, 9).Value = dataRow["time_out_formatted"].ToString();
                            row++;
                        }

                        // Auto-fit columns
                        worksheet.Columns().AdjustToContents();

                        // Add alternating row colors
                        for (int i = 2; i <= row - 1; i++)
                        {
                            if (i % 2 == 0)
                            {
                                worksheet.Range(i, 1, i, 9).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EAF6");
                            }
                        }

                        // Add border to all cells
                        worksheet.Range(1, 1, row - 1, 9).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        worksheet.Range(1, 1, row - 1, 9).Style.Border.InsideBorder = XLBorderStyleValues.Thin;

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show($"Data exported successfully!\n\nRecords exported: {filteredView.Count}\nFile: {saveFileDialog.FileName}",
                        "Export Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting to Excel: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region UI Events

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadAllVisitors();
            MessageBox.Show("Data refreshed successfully!",
                "Refresh",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DgAdminVisitors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAdminVisitors.SelectedItem != null)
            {
                var selectedRow = (DataRowView)dgAdminVisitors.SelectedItem;
                selectedVisitorId = Convert.ToInt32(selectedRow["id"]);
                btnEdit.IsEnabled = true;
                btnDelete.IsEnabled = true;
            }
            else
            {
                selectedVisitorId = -1;
                btnEdit.IsEnabled = false;
                btnDelete.IsEnabled = false;
            }
        }

        #endregion
    }
}