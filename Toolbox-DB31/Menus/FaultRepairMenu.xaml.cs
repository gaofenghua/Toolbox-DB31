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
using Toolbox_DB31.Classes;

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for FaultRepairMenu.xaml
    /// </summary>
    public partial class FaultRepairMenu : Page
    {
        public FaultRepairMenuViewModel m_ViewModel = new FaultRepairMenuViewModel();

        public FaultRepairMenu()
        {
            InitializeComponent();
            m_ViewModel = (FaultRepairMenuViewModel)DataContext;
        }

        public string Get_Notes()
        {
            string sNote = "视频监控故障：";
            if (m_ViewModel.IsMatrixVideoFailureEnabled == true)
            {
                //sNote += "视频丢失 ";
            }

            sNote += m_ViewModel.VideoMonitorRecords;

            return sNote;
        }
    }
}
