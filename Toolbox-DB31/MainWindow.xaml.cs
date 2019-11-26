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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DevExpress.Xpf.Core.ThemedWindow
    {
        public static RoutedCommand myCommand = new RoutedCommand();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnNew_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
           

        }
        private void myCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //Actions to perform 
           
        }
        private void myCommandExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Case of command's availability 
            if (true)
            {
                e.CanExecute = true;
            }
        }
    }
}
