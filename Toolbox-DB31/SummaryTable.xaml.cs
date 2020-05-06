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

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for SummaryTable.xaml
    /// </summary>
    public partial class SummaryTable : Page
    {
        public SummaryTable()
        {
            InitializeComponent();
            GridControl_Summary.Columns["状态"].Visible = true;
            GridControl_Summary.Columns["IsSelected"].Header = "图像上传";
            GridControl_Summary.Columns["IsSelected"].HorizontalHeaderContentAlignment = System.Windows.HorizontalAlignment.Center;

            GridControl_Summary.Columns["AlarmEnable"].Header = "报警激活";
            GridControl_Summary.Columns["AlarmEnable"].HorizontalHeaderContentAlignment = System.Windows.HorizontalAlignment.Center;
        }
    }
}
