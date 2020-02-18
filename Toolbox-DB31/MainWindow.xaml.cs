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
using Toolbox_DB31.AVMS_Adapter;

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DevExpress.Xpf.Core.ThemedWindow
    {
        public static RoutedCommand myCommand = new RoutedCommand();

        LoginPage theLoginPage;

        public MainWindow()
        {
            InitializeComponent();
            Global.g_Main_ViewModel = (Main_ViewModel) DataContext;

            theLoginPage = new LoginPage();
            theLoginPage.Event_Login_Finished += OnEvent_Login_Finished;
            frmMain.NavigationService.Navigate(theLoginPage);

            //Hide the bottom information
            Global.g_Main_ViewModel.BottomLabel = "";
            simpleButton.Visibility = Visibility.Hidden;

            //Go to the default NavBar item
            //myNavBarControl.ActiveGroup = myNavBarControl.Groups[0];
            //myNavBarControl.SelectedItem = myNavBarControl.Groups[0].Items[0];
            //myNavBarControl.ActiveGroup = navBarGroup_system;
            myNavBarControl.SelectedItem = navBarGroup_system.Items[0];
           
            //DB31_Controller controller = new DB31_Controller();
            //controller.StartHeartbeat(); 

            //AVMS_Com avms = new AVMS_Com();
            //AVMSAdapter adapter = new AVMSAdapter();
            //adapter.Start("127.0.0.1","admin","admin", "0010123033030");
            //adapter.AVMSTriggered += new AVMSAdapter.AVMSTriggeredHandler(HandleAVMSEvent);
        }

        //private void HandleAVMSEvent(object sender, AVMSEventArgs e)
        //{
        //    AVMS_ALARM alarmType = e.m_alarmType;
        //    DateTime alarmTime = e.m_alarmTime;
        //    int channelId = -1;
        //    int.TryParse(e.m_cameraId.ToString(), out channelId);
        //    string picData = e.m_pictureData;
            
        //}

        private void btnNew_ItemClick(object sender, DevExpress.Xpf.Bars.ItemClickEventArgs e)
        {
           

        }
        private void myCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //Actions to perform 
            //Global.g_CameraList.Add(new Camera_Model() { Name = "From command", IsSelected = true });
            
            //Global.g_Main_ViewModel.BottomLabel = "当前用户：建设单位\r\n当前时间：";
            frmMain.NavigationService.Navigate(theLoginPage);

            myNavBarControl.ActiveGroup = navBarGroup_system;
            myNavBarControl.SelectedItem = navBarGroup_system.Items[0];

            Global.g_Main_ViewModel.BottomLabel = "";
            simpleButton.Visibility = Visibility.Hidden;
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
        private void OnEvent_Login_Finished(object sender,string sRet)
        {
            frmMain.NavigationService.Navigate(new SummaryTable());

            myNavBarControl.SelectedItem = navBarItem_ImageUpload;
            simpleButton.Visibility = Visibility.Visible;
            //myNavBarControl.ActiveGroup = myNavBarControl.Groups[1];
            //myNavBarControl.SelectedItem = myNavBarControl.Groups[1].Items[0];
        }

        private void navBarItem41_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new SummaryTable());

            myNavBarControl.ActiveGroup = myNavBarControl.Groups[3];
            myNavBarControl.SelectedItem = myNavBarControl.Groups[3].Items[0];

            simpleButton.Visibility = Visibility.Visible;
        }
        private void navBarItem_ImageUpload_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new SummaryTable());

            myNavBarControl.SelectedItem = navBarItem_ImageUpload;

            simpleButton.Visibility = Visibility.Visible;
        }
    }
}
