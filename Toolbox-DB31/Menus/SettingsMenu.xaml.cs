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
using Toolbox_DB31.Classes;

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for SettingsMenu.xaml
    /// </summary>
    public partial class SettingsMenu : Page
    {
        public SettingsMenuViewModel m_ViewModel = new SettingsMenuViewModel();

        public SettingsMenu()
        {
            InitializeComponent();
            editDailyUpdateDateTime.Text = DateTime.Now.ToString();
            SetButton(m_ViewModel.IsAlarmListeningEnabled());
            m_ViewModel = DataContext as SettingsMenuViewModel;
        }

        private void btnUploadData_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnUploadAlarm_Click(object sender, RoutedEventArgs e)
        {
            m_ViewModel.UploadAlarmLog();
        }

        private void btnStartListening_Click(object sender, RoutedEventArgs e)
        {
            m_ViewModel.StartAlarmListening();
            SetButton(m_ViewModel.IsAlarmListeningEnabled());
        }

        private void btnStopListening_Click(object sender, RoutedEventArgs e)
        {
            m_ViewModel.StopAlarmListening();
            SetButton(m_ViewModel.IsAlarmListeningEnabled());
        }

        private void SetButton(bool isAlarmListeningEnabled)
        {
            btnUploadData.IsEnabled = true;
            btnUploadAlarm.IsEnabled = true;
            btnStartListening.IsEnabled = !isAlarmListeningEnabled;
            btnStopListening.IsEnabled = isAlarmListeningEnabled;
        }
    }
}
