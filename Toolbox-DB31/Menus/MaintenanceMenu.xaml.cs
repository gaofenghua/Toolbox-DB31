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
using Toolbox_DB31.Classes;

namespace Toolbox_DB31
{
	/// <summary>
	/// Interaction logic for MaintenanceMenu.xaml
	/// </summary>
	public partial class MaintenanceMenu : Page
	{
        public MaintenanceMenuViewModel m_ViewModel = new MaintenanceMenuViewModel();

        public MaintenanceMenu()
		{
			InitializeComponent();
            m_ViewModel = DataContext as MaintenanceMenuViewModel;
		}

        private void rdoRegular_Selected(object sender, RoutedEventArgs e)
        {
            m_ViewModel.m_Type = MaintenanceType.REGULAR_MAINTENANCE;
        }

        private void rdoSpecial_Selected(object sender, RoutedEventArgs e)
        {
            m_ViewModel.m_Type = MaintenanceType.SPECIAL_MAINTENANCE;
        }

        public string Get_Notes()
        {
            string sNote = "";
            if(m_ViewModel.IsVideoMonitorEnabled == true)
            {
                sNote += "视频监控 ";
            }

            sNote += m_ViewModel.MaintenanceRecords;

            return sNote;
        }
    }
}
