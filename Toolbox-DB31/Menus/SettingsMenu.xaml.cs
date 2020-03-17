using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Editors;

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for SettingsMenu.xaml
    /// </summary>
    public partial class SettingsMenu : Page
    {
        public SettingsMenu()
        {
            InitializeComponent();
            editDailyUpdateDateTime.Text = DateTime.Now.ToString();
        }

        private void btnUploadData_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnUploadAlarm_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnStartListening_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnStopListening_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
