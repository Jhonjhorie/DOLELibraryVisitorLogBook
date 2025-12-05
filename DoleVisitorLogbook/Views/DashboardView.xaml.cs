using DoleVisitorLogbook.Database;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DoleVisitorLogbook.Views
{
    public partial class DashboardView : UserControl
    {
        private DispatcherTimer? refreshTimer;
        private List<DailyStats> weeklyData;

        public DashboardView()
        {
            InitializeComponent();
            weeklyData = new List<DailyStats>();

            // Load data after the control is fully loaded
            this.Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
            InitializeAutoRefresh();
        }

        #region Data Loading

        private void LoadDashboardData()
        {
            try
            {
                LoadStatistics();
                LoadRecentActivity();
                LoadWeeklyChart();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard data: {ex.Message}\n\nStack Trace: {ex.StackTrace}",
                    "Dashboard Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();

                    // Today's Visitors
                    int todayCount = GetVisitorCount(conn, DateTime.Today, DateTime.Today.AddDays(1));
                    int yesterdayCount = GetVisitorCount(conn, DateTime.Today.AddDays(-1), DateTime.Today);
                    txtTodayVisitors.Text = todayCount.ToString();
                    UpdateTrendText(txtTodayVisitors, todayCount, yesterdayCount, "from yesterday");

                    // This Week
                    DateTime weekStart = GetStartOfWeek(DateTime.Today);
                    DateTime lastWeekStart = weekStart.AddDays(-7);
                    int thisWeekCount = GetVisitorCount(conn, weekStart, DateTime.Now);
                    int lastWeekCount = GetVisitorCount(conn, lastWeekStart, weekStart);
                    txtWeekVisitors.Text = thisWeekCount.ToString();
                    UpdateTrendText(txtWeekVisitors, thisWeekCount, lastWeekCount, "from last week");

                    // This Month
                    DateTime monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    DateTime lastMonthStart = monthStart.AddMonths(-1);
                    int thisMonthCount = GetVisitorCount(conn, monthStart, DateTime.Now);
                    int lastMonthCount = GetVisitorCount(conn, lastMonthStart, monthStart);
                    txtMonthVisitors.Text = thisMonthCount.ToString("N0");
                    UpdateTrendText(txtMonthVisitors, thisMonthCount, lastMonthCount, "from last month");

                    // Active Now (visitors who checked in today but haven't checked out)
                    int activeCount = GetActiveVisitors(conn);
                    txtActiveVisitors.Text = activeCount.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading statistics: {ex.Message}",
                    "Statistics Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private int GetVisitorCount(MySqlConnection conn, DateTime startDate, DateTime endDate)
        {
            try
            {
                string sql = @"SELECT COUNT(*) FROM visitors 
                              WHERE time_in >= @StartDate AND time_in < @EndDate";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@StartDate", startDate);
                    cmd.Parameters.AddWithValue("@EndDate", endDate);

                    object result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetVisitorCount Error: {ex.Message}");
                return 0;
            }
        }

        private int GetActiveVisitors(MySqlConnection conn)
        {
            try
            {
                // This query avoids comparing time_out as DATETIME to prevent errors
                // It checks if time_out is NULL or an empty/invalid string
                string sql = @"SELECT COUNT(*) FROM visitors 
                              WHERE DATE(time_in) = @Today 
                              AND (time_out IS NULL OR time_out = '' OR CAST(time_out AS CHAR) = '')";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@Today", DateTime.Today.ToString("yyyy-MM-dd"));

                    object result = cmd.ExecuteScalar();
                    int count = result != null ? Convert.ToInt32(result) : 0;

                    System.Diagnostics.Debug.WriteLine($"Active Visitors: {count} (Date: {DateTime.Today:yyyy-MM-dd})");
                    return count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetActiveVisitors Error: {ex.Message}");

                // Try alternative query if the first one fails
                try
                {
                    // Fallback: Check only for NULL
                    string fallbackSql = @"SELECT COUNT(*) FROM visitors 
                                          WHERE DATE(time_in) = @Today 
                                          AND time_out IS NULL";

                    using (MySqlCommand cmd = new MySqlCommand(fallbackSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Today", DateTime.Today.ToString("yyyy-MM-dd"));
                        object result = cmd.ExecuteScalar();
                        return result != null ? Convert.ToInt32(result) : 0;
                    }
                }
                catch
                {
                    MessageBox.Show($"Error getting active visitors: {ex.Message}\n\nPlease check your database structure.",
                        "Active Visitors Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return 0;
                }
            }
        }

        private void UpdateTrendText(TextBlock statBlock, int current, int previous, string suffix)
        {
            try
            {
                // Find the parent StackPanel
                var parent = statBlock.Parent as StackPanel;
                if (parent == null) return;

                // Find the last TextBlock in the StackPanel (which should be the trend text)
                TextBlock? trendBlock = null;
                foreach (var child in parent.Children)
                {
                    if (child is TextBlock tb && tb != statBlock)
                    {
                        trendBlock = tb;
                    }
                }

                if (trendBlock == null) return;

                if (previous == 0)
                {
                    trendBlock.Text = current > 0 ? "New data" : "No data";
                    trendBlock.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
                    return;
                }

                double percentChange = ((double)(current - previous) / previous) * 100;
                string sign = percentChange >= 0 ? "+" : "";
                trendBlock.Text = $"{sign}{percentChange:F1}% {suffix}";

                // Set color based on trend
                if (percentChange > 0)
                    trendBlock.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Green
                else if (percentChange < 0)
                    trendBlock.Foreground = new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Red
                else
                    trendBlock.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)); // Gray
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateTrendText Error: {ex.Message}");
            }
        }

        #endregion

        #region Recent Activity

        private void LoadRecentActivity()
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();
                    string sql = @"SELECT name, client_type, time_in 
                                  FROM visitors 
                                  WHERE DATE(time_in) = @Today 
                                  ORDER BY time_in DESC 
                                  LIMIT 10";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Today", DateTime.Today.ToString("yyyy-MM-dd"));

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Find the ScrollViewer in the Recent Activity section
                            var scrollViewer = FindName("recentActivityScroll") as ScrollViewer;

                            // If not found by name, search the visual tree
                            if (scrollViewer == null)
                            {
                                scrollViewer = FindScrollViewerInVisualTree(this);
                            }

                            if (scrollViewer == null)
                            {
                                System.Diagnostics.Debug.WriteLine("ScrollViewer not found!");
                                return;
                            }

                            var activityPanel = scrollViewer.Content as StackPanel;
                            if (activityPanel == null)
                            {
                                // Create a new StackPanel if it doesn't exist
                                activityPanel = new StackPanel();
                                scrollViewer.Content = activityPanel;
                            }

                            activityPanel.Children.Clear();

                            int count = 0;
                            while (reader.Read())
                            {
                                string name = reader["name"]?.ToString() ?? "Unknown";
                                string clientType = reader["client_type"]?.ToString() ?? "Guest";

                                DateTime timeIn = DateTime.Now;
                                if (reader["time_in"] != DBNull.Value)
                                {
                                    timeIn = Convert.ToDateTime(reader["time_in"]);
                                }

                                var activityItem = CreateActivityItem(name, clientType, timeIn, count < 9);
                                activityPanel.Children.Add(activityItem);
                                count++;
                            }

                            if (count == 0)
                            {
                                var noDataText = new TextBlock
                                {
                                    Text = "No check-ins today yet",
                                    Foreground = new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                                    FontStyle = FontStyles.Italic,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Margin = new Thickness(0, 20, 0, 0)
                                };
                                activityPanel.Children.Add(noDataText);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading recent activity: {ex.Message}\n\n{ex.StackTrace}",
                    "Activity Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private Border CreateActivityItem(string name, string clientType, DateTime timeIn, bool showBorder)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224)),
                BorderThickness = showBorder ? new Thickness(0, 0, 0, 1) : new Thickness(0),
                Padding = new Thickness(0, 10, 0, 10)
            };

            var stackPanel = new StackPanel();

            var nameText = new TextBlock
            {
                Text = name ?? "Unknown",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 58, 111))
            };

            var detailsText = new TextBlock
            {
                Text = $"{clientType ?? "Unknown"} • {timeIn:hh:mm tt}",
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102)),
                FontSize = 12,
                Margin = new Thickness(0, 3, 0, 0)
            };

            stackPanel.Children.Add(nameText);
            stackPanel.Children.Add(detailsText);
            border.Child = stackPanel;

            return border;
        }

        private ScrollViewer? FindScrollViewerInVisualTree(DependencyObject parent)
        {
            if (parent == null) return null;

            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is ScrollViewer scrollViewer)
                {
                    // Make sure it's the right ScrollViewer (height around 280)
                    if (scrollViewer.Height >= 250 && scrollViewer.Height <= 300)
                        return scrollViewer;
                }

                var result = FindScrollViewerInVisualTree(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        #endregion

        #region Weekly Chart

        private void LoadWeeklyChart()
        {
            try
            {
                weeklyData.Clear();
                using (var conn = DB.GetConnection())
                {
                    conn.Open();

                    // Get last 7 days of data
                    for (int i = 6; i >= 0; i--)
                    {
                        DateTime date = DateTime.Today.AddDays(-i);
                        int count = GetVisitorCount(conn, date, date.AddDays(1));

                        weeklyData.Add(new DailyStats
                        {
                            Date = date,
                            DayName = date.ToString("ddd"),
                            VisitorCount = count,
                            IsWeekend = date.DayOfWeek == DayOfWeek.Saturday ||
                                       date.DayOfWeek == DayOfWeek.Sunday
                        });

                        System.Diagnostics.Debug.WriteLine($"Chart Data: {date:MMM dd} - {count} visitors");
                    }
                }

                UpdateChartVisuals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading weekly chart: {ex.Message}",
                    "Chart Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void UpdateChartVisuals()
        {
            try
            {
                if (weeklyData.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("No weekly data to display");
                    return;
                }

                int maxCount = weeklyData.Max(d => d.VisitorCount);
                if (maxCount == 0) maxCount = 1; // Avoid division by zero

                const double maxHeight = 180; // Maximum bar height
                const double minHeight = 30;  // Minimum visible height

                System.Diagnostics.Debug.WriteLine($"Updating chart with max count: {maxCount}");

                // Update each day's bar using named elements
                for (int i = 0; i < Math.Min(7, weeklyData.Count); i++)
                {
                    var data = weeklyData[i];
                    int dayNum = i + 1;

                    // Find the bar, count, and day label by name
                    var bar = FindName($"bar{dayNum}") as Rectangle;
                    var countText = FindName($"count{dayNum}") as TextBlock;
                    var dayLabel = FindName($"day{dayNum}") as TextBlock;

                    if (bar != null)
                    {
                        // Calculate height with better proportions
                        double height = maxCount > 0
                            ? Math.Max(minHeight, (data.VisitorCount / (double)maxCount) * maxHeight)
                            : minHeight;

                        bar.Height = height;

                        // Set color based on weekend/weekday
                        Color barColor = data.IsWeekend
                            ? Color.FromRgb(102, 187, 106)  // Green for weekend
                            : Color.FromRgb(92, 107, 192);   // Blue for weekday

                        bar.Fill = new SolidColorBrush(barColor);

                        // Update the glow effect color
                        if (bar.Effect is DropShadowEffect shadow)
                        {
                            shadow.Color = barColor;
                        }
                    }

                    if (countText != null)
                    {
                        countText.Text = data.VisitorCount.ToString();
                        // Show/hide count based on bar height
                        countText.Visibility = bar != null && bar.Height >= 25
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }

                    if (dayLabel != null)
                    {
                        dayLabel.Text = data.DayName;
                    }

                    System.Diagnostics.Debug.WriteLine(
                        $"Updated day {dayNum}: {data.DayName} - {data.VisitorCount} visitors (height: {bar?.Height:F0}px)");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateChartVisuals Error: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error updating chart: {ex.Message}", "Chart Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        #endregion

        #region Auto Refresh

        private void InitializeAutoRefresh()
        {
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMinutes(1); // Refresh every minute
            refreshTimer.Tick += RefreshTimer_Tick;
            refreshTimer.Start();
        }

        private void RefreshTimer_Tick(object? sender, EventArgs e)
        {
            // Only refresh active visitors and recent activity (not full reload)
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();
                    int activeCount = GetActiveVisitors(conn);
                    txtActiveVisitors.Text = activeCount.ToString();
                }

                LoadRecentActivity();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auto-refresh error: {ex.Message}");
            }
        }

        public void StopAutoRefresh()
        {
            refreshTimer?.Stop();
        }

        public void ManualRefresh()
        {
            LoadDashboardData();
        }

        #endregion

        #region Helper Methods

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        #endregion

        #region UI Event Handlers

        private void BtnRefreshDashboard_Click(object sender, RoutedEventArgs e)
        {
            ManualRefresh();
            MessageBox.Show("Dashboard data refreshed successfully!",
                "Refresh Complete",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void BtnDebugActive_Click(object sender, RoutedEventArgs e)
        {
            DebugActiveVisitors();
        }

        // TEMPORARY DEBUG METHOD - Remove after fixing
        private void DebugActiveVisitors()
        {
            try
            {
                using (var conn = DB.GetConnection())
                {
                    conn.Open();

                    // Get all visitors from today - read time_out as string to avoid conversion errors
                    string sql = @"SELECT id, name, time_in, 
                                  CAST(time_out AS CHAR) as time_out_string,
                                  (time_out IS NULL) as is_null,
                                  (time_out = '') as is_empty
                                  FROM visitors 
                                  WHERE DATE(time_in) = @Today 
                                  ORDER BY time_in DESC 
                                  LIMIT 20";

                    using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Today", DateTime.Today.ToString("yyyy-MM-dd"));

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            string debugInfo = $"Today's Visitors ({DateTime.Today:yyyy-MM-dd}):\n\n";
                            int activeCount = 0;

                            while (reader.Read())
                            {
                                string name = reader["name"]?.ToString() ?? "Unknown";
                                string timeIn = reader["time_in"]?.ToString() ?? "N/A";
                                string timeOutStr = reader["time_out_string"]?.ToString() ?? "NULL";
                                bool isNull = Convert.ToBoolean(reader["is_null"]);
                                bool isEmpty = reader["is_empty"] != DBNull.Value && Convert.ToBoolean(reader["is_empty"]);

                                bool isActive = isNull || isEmpty || string.IsNullOrWhiteSpace(timeOutStr);

                                if (isActive) activeCount++;

                                debugInfo += $"{name}\n";
                                debugInfo += $"  In: {timeIn}\n";
                                debugInfo += $"  Out String: '{timeOutStr}'\n";
                                debugInfo += $"  Is NULL: {isNull}\n";
                                debugInfo += $"  Is Empty: {isEmpty}\n";
                                debugInfo += $"  Status: {(isActive ? "✓ ACTIVE" : "× Checked Out")}\n\n";
                            }

                            debugInfo += $"\n═══════════════════\n";
                            debugInfo += $"Total Active: {activeCount}";

                            MessageBox.Show(debugInfo, "Debug: Active Visitors",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Debug Error: {ex.Message}\n\n{ex.StackTrace}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Cleanup

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            StopAutoRefresh();
        }

        #endregion
    }

    #region Helper Classes

    public class DailyStats
    {
        public DateTime Date { get; set; }
        public string DayName { get; set; } = string.Empty;
        public int VisitorCount { get; set; }
        public bool IsWeekend { get; set; }
    }

    #endregion
}