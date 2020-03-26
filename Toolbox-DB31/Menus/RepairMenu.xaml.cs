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
	/// Interaction logic for RepairMenu.xaml
	/// </summary>
	public partial class RepairMenu : Page
	{
        public RepairMenuViewModel m_ViewModel = new RepairMenuViewModel();

        public RepairMenu()
		{
			InitializeComponent();
            m_ViewModel = DataContext as RepairMenuViewModel;
        }

        private void rdoNotRepired_Selected(object sender, RoutedEventArgs e)
        {
            m_ViewModel.m_Status = RepairStatus.NOT_REPAIR;
        }

        private void rdoPartRepired_Selected(object sender, RoutedEventArgs e)
        {
            m_ViewModel.m_Status = RepairStatus.PART_REPAIR;
        }

        private void rdoTotalRepired_Selected(object sender, RoutedEventArgs e)
        {
            m_ViewModel.m_Status = RepairStatus.TOTAL_REPAIR;
        }
    }
}
