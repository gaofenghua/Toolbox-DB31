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
        private MainWindow m_parent = null;

        public SettingsMenu(object parent)
        {
            InitializeComponent();
            SetButton(m_ViewModel.IsAlarmListeningEnabled());
            m_ViewModel = DataContext as SettingsMenuViewModel;
            m_parent = parent as MainWindow;
        }

        private void btnUploadData_Click(object sender, RoutedEventArgs e)
        {
            foreach (Camera_Model cam in Global.g_CameraList)
            {
                if ("在线" == cam.Status)
                {
                    int camId = cam.CameraID;
                    string log = m_ViewModel.GetEventLog(camId);
                    m_parent.UploadEventLog(camId, log);

                    break;
                }
            }
        }

        private void btnUploadAlarm_Click(object sender, RoutedEventArgs e)
        {
            foreach (Camera_Model cam in Global.g_CameraList)
            {
                if (true == cam.IsSelected)
                {
                    int camId = cam.CameraID;
                    string log = m_ViewModel.GetAlarmLog(camId);
                    m_parent.UploadAlarmLog(camId, log);
                }
            }
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

        private void CheckEdit_EditValueChanged(object sender, EditValueChangedEventArgs e)
        {
            editDailyUpdateDateTime.IsEnabled = !m_ViewModel.IsDailyTimerEnabled;
            m_parent.SetDailyTimer(m_ViewModel.IsDailyTimerEnabled);
        }
    }
}
