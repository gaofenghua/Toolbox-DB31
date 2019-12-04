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
using Toolbox_DB31.DB31_Adapter;

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
            Global.g_Main_ViewModel = (Main_ViewModel) DataContext;

            DB31_Controller controller = new DB31_Controller();
            controller.StartHeartbeat(); 
        }

        private void btnNew_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
           

        }
        private void myCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //Actions to perform 
            Global.g_CameraList.Add(new Camera_Model() { Name = "From command", IsSelected = true });
            
            Global.g_Main_ViewModel.BottomLabel = "当前用户：建设单位\r\n当前时间：";

        }
        private void myCommandExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Case of command's availability 
            if (true)
            {
                e.CanExecute = true;
            }
        }

        private void simpleButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
