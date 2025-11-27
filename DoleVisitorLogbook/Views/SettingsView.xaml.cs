using System;
using System.Collections.Generic;
using System.Linq;
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

namespace DoleVisitorLogbook.Views
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
        }
        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Add user functionality not yet implemented.");
        }

        private void RemoveUser_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Remove user functionality not yet implemented.");
        }

        private void ChangePassword_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Change password functionality not yet implemented.");
        }
    }
}
