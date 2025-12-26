using DoleVisitorLogbook.Database;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace DoleVisitorLogbook.Views
{
    public partial class Reports : UserControl
    {
        private DataTable reportData = new DataTable();

        public Reports()
        {
            InitializeComponent();
            InitializeReportTypes();
            SetDefaultDates();
        }

        #region Initialization

        private void InitializeReportTypes()
        {
            cmbReportType.Items.Add("Daily Report");
            cmbReportType.Items.Add("Weekly Report");
            cmbReportType.Items.Add("Monthly Report");
            cmbReportType.Items.Add("Custom Date Range");
            cmbReportType.Items.Add("By Gender");
            cmbReportType.Items.Add("By Client Type");
            cmbReportType.Items.Add("By Purpose");
            cmbReportType.SelectedIndex = 0;
        }

        private void SetDefaultDates()
        {
            dpReportDateFrom.SelectedDate = DateTime.Today;
            dpReportDateTo.SelectedDate = DateTime.Today;
        }

        #endregion

        #region Report Type Selection

        private void CmbReportType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbReportType.SelectedItem == null) return;

            string? selectedReport = cmbReportType.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedReport)) return;

            switch (selectedReport)
            {
                case "Daily Report":
                    dpReportDateFrom.SelectedDate = DateTime.Today;
                    dpReportDateTo.SelectedDate = DateTime.Today;
                    dpReportDateFrom.IsEnabled = true;
                    dpReportDateTo.IsEnabled = false;
                    break;

                case "Weekly Report":
                    dpReportDateFrom.SelectedDate = DateTime.Today.AddDays(-7);
                    dpReportDateTo.SelectedDate = DateTime.Today;
                    dpReportDateFrom.IsEnabled = true;
                    dpReportDateTo.IsEnabled = false;
                    break;

                case "Monthly Report":
                    dpReportDateFrom.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    dpReportDateTo.SelectedDate = DateTime.Today;
                    dpReportDateFrom.IsEnabled = true;
                    dpReportDateTo.IsEnabled = false;
                    break;

                case "Custom Date Range":
                    dpReportDateFrom.IsEnabled = true;
                    dpReportDateTo.IsEnabled = true;
                    break;

                default:
                    dpReportDateFrom.SelectedDate = DateTime.Today.AddMonths(-1);
                    dpReportDateTo.SelectedDate = DateTime.Today;
                    dpReportDateFrom.IsEnabled = true;
                    dpReportDateTo.IsEnabled = true;
                    break;
            }
        }

        #endregion

        #region Generate Report

        private void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
        {
            if (cmbReportType.SelectedItem == null)
            {
                MessageBox.Show("Please select a report type.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!dpReportDateFrom.SelectedDate.HasValue || !dpReportDateTo.SelectedDate.HasValue)
            {
                MessageBox.Show("Please select valid date range.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            string? reportType = cmbReportType.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(reportType))
            {
                GenerateReport(reportType);
            }
        }

        private void GenerateReport(string reportType)
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();

                    if (!dpReportDateFrom.SelectedDate.HasValue || !dpReportDateTo.SelectedDate.HasValue)
                    {
                        MessageBox.Show("Please select valid date range.",
                            "Validation Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    DateTime dateFrom = dpReportDateFrom.SelectedDate.Value.Date;
                    DateTime dateTo = dpReportDateTo.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

                    string sql = "";

                    switch (reportType)
                    {
                        case "By Gender":
                            sql = @"SELECT gender AS 'Category', 
                                   COUNT(*) AS 'Total Visitors',
                                   COUNT(CASE WHEN time_out IS NOT NULL THEN 1 END) AS 'Completed Visits',
                                   COUNT(CASE WHEN time_out IS NULL THEN 1 END) AS 'Active Visits'
                                   FROM visitors 
                                   WHERE time_in >= @DateFrom AND time_in <= @DateTo
                                   GROUP BY gender
                                   ORDER BY COUNT(*) DESC";
                            break;

                        case "By Client Type":
                            sql = @"SELECT client_type AS 'Category', 
                                   COUNT(*) AS 'Total Visitors',
                                   COUNT(CASE WHEN time_out IS NOT NULL THEN 1 END) AS 'Completed Visits',
                                   COUNT(CASE WHEN time_out IS NULL THEN 1 END) AS 'Active Visits'
                                   FROM visitors 
                                   WHERE time_in >= @DateFrom AND time_in <= @DateTo
                                   GROUP BY client_type
                                   ORDER BY COUNT(*) DESC";
                            break;

                        case "By Purpose":
                            sql = @"SELECT purpose AS 'Category', 
                                   COUNT(*) AS 'Total Visitors',
                                   COUNT(CASE WHEN time_out IS NOT NULL THEN 1 END) AS 'Completed Visits',
                                   COUNT(CASE WHEN time_out IS NULL THEN 1 END) AS 'Active Visits'
                                   FROM visitors 
                                   WHERE time_in >= @DateFrom AND time_in <= @DateTo
                                   GROUP BY purpose
                                   ORDER BY COUNT(*) DESC";
                            break;

                        default:
                            sql = @"SELECT 
                                   DATE(time_in) AS 'Date',
                                   COUNT(*) AS 'Total Visitors',
                                   COUNT(CASE WHEN gender='Male' THEN 1 END) AS 'Male',
                                   COUNT(CASE WHEN gender='Female' THEN 1 END) AS 'Female',
                                   COUNT(CASE WHEN time_out IS NOT NULL THEN 1 END) AS 'Completed',
                                   COUNT(CASE WHEN time_out IS NULL THEN 1 END) AS 'Active'
                                   FROM visitors 
                                   WHERE time_in >= @DateFrom AND time_in <= @DateTo
                                   GROUP BY DATE(time_in)
                                   ORDER BY DATE(time_in) DESC";
                            break;
                    }

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@DateFrom", dateFrom);
                        cmd.Parameters.AddWithValue("@DateTo", dateTo);

                        MySqlDataAdapter da = new MySqlDataAdapter(cmd);
                        reportData = new DataTable();
                        da.Fill(reportData);

                        dgReportData.ItemsSource = reportData.DefaultView;

                        if (reportData.Rows.Count == 0)
                        {
                            MessageBox.Show("No data found for the selected criteria.",
                                "No Data",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error generating report: {ex.Message}",
                    "Report Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Export Functions

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (reportData == null || reportData.Rows.Count == 0)
            {
                MessageBox.Show("No report data to export. Please generate a report first.",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Files|*.xlsx",
                    Title = "Export Report",
                    FileName = $"VisitorReport_{cmbReportType.SelectedItem}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Report");

                        // Add report header
                        worksheet.Cell(1, 1).Value = "DOLE Visitor Logbook Report";
                        worksheet.Cell(1, 1).Style.Font.Bold = true;
                        worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                        worksheet.Cell(1, 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#5C6BC0");
                        worksheet.Cell(1, 1).Style.Font.FontColor = XLColor.White;

                        worksheet.Cell(2, 1).Value = $"Report Type: {cmbReportType.SelectedItem?.ToString() ?? "N/A"}";
                        worksheet.Cell(3, 1).Value = $"Date Range: {dpReportDateFrom.SelectedDate?.ToString("MM/dd/yyyy") ?? "N/A"} to {dpReportDateTo.SelectedDate?.ToString("MM/dd/yyyy") ?? "N/A"}";
                        worksheet.Cell(4, 1).Value = $"Generated: {DateTime.Now:MM/dd/yyyy hh:mm tt}";

                        // Add data table
                        int dataStartRow = 6;

                        // Headers
                        for (int col = 0; col < reportData.Columns.Count; col++)
                        {
                            worksheet.Cell(dataStartRow, col + 1).Value = reportData.Columns[col].ColumnName;
                            worksheet.Cell(dataStartRow, col + 1).Style.Font.Bold = true;
                            worksheet.Cell(dataStartRow, col + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#5C6BC0");
                            worksheet.Cell(dataStartRow, col + 1).Style.Font.FontColor = XLColor.White;
                        }

                        // Data
                        for (int row = 0; row < reportData.Rows.Count; row++)
                        {
                            for (int col = 0; col < reportData.Columns.Count; col++)
                            {
                                worksheet.Cell(dataStartRow + row + 1, col + 1).Value = reportData.Rows[row][col].ToString();
                            }
                        }

                        // Formatting
                        worksheet.Columns().AdjustToContents();

                        // Add alternating row colors
                        for (int i = dataStartRow + 1; i <= dataStartRow + reportData.Rows.Count; i++)
                        {
                            if ((i - dataStartRow) % 2 == 0)
                            {
                                worksheet.Range(i, 1, i, reportData.Columns.Count).Style.Fill.BackgroundColor = XLColor.FromHtml("#F3E5F5");
                            }
                        }

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show($"Report exported successfully!\n\nFile: {saveFileDialog.FileName}",
                        "Export Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting report: {ex.Message}",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnPrintReport_Click(object sender, RoutedEventArgs e)
        {
            if (reportData == null || reportData.Rows.Count == 0)
            {
                MessageBox.Show("No report data to print. Please generate a report first.",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {
                    printDialog.PrintVisual(dgReportData, "DOLE Visitor Logbook Report");
                    MessageBox.Show("Report sent to printer successfully!",
                        "Print Successful",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error printing report: {ex.Message}",
                    "Print Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #endregion

        #region Clear Report

        private void BtnClearReport_Click(object sender, RoutedEventArgs e)
        {
            reportData = new DataTable();
            dgReportData.ItemsSource = null;
            cmbReportType.SelectedIndex = 0;
            SetDefaultDates();
        }

        #endregion
    }
}