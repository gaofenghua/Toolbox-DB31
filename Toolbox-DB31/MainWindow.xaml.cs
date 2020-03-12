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
using System.Threading;
using System.Diagnostics;

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DevExpress.Xpf.Core.ThemedWindow
    {
        public static RoutedCommand myCommand = new RoutedCommand();

        LoginPage theLoginPage;

        enum Menu_Item { User_Login, Inspect_Image_Upload,Test_Image_Upload, Maintenance_Report, Repair_Report};
        Menu_Item Current_Menu_Item;

        DB31_Controller db31;

        public MainWindow()
        {
            InitializeComponent();

            Global.g_Main_ViewModel = (Main_ViewModel)DataContext;
            
            theLoginPage = new LoginPage();
            theLoginPage.Event_Login_Finished += OnEvent_Login_Finished;
            frmMain.NavigationService.Navigate(theLoginPage);
            Current_Menu_Item = Menu_Item.User_Login;

            //Hide the bottom information
            Set_Button_Label(false);

            //Go to the default NavBar item
            //myNavBarControl.ActiveGroup = myNavBarControl.Groups[0];
            //myNavBarControl.SelectedItem = myNavBarControl.Groups[0].Items[0];
            //myNavBarControl.ActiveGroup = navBarGroup_system;
            myNavBarControl.SelectedItem = navBarGroup_system.Items[0];
           
            //DB31_Controller controller = new DB31_Controller();
            //controller.StartHeartbeat(); 
            db31 = new DB31_Controller(Global.g_User);
            db31.Working_Message += OnEvent_Working_Message;

            DeviceSummary.CfgFilePath = @".\Configuration.csv";
            AVMSAdapter adapter = new AVMSAdapter();
            adapter.Start("127.0.0.1", "admin", "admin", "0010123033030");
            adapter.AVMSTriggered += new AVMSAdapter.AVMSTriggeredHandler(HandleAVMSEvent);

            Global.g_VMS_Adapter = adapter;
        }

        private void HandleAVMSEvent(object sender, AVMSEventArgs e)
        {
            AVMS_ALARM alarmType = e.m_alarmType;
            DateTime alarmTime = e.m_alarmTime;
            int channelId = -1;
            int.TryParse(e.m_cameraId.ToString(), out channelId);
            string picData = e.m_pictureData;

        }


        private void Set_Button_Label(bool bVisible)
        {
            if (true == bVisible)
            {
                Button_Cancel.Visibility = Visibility.Visible;
                Button_Upload.Visibility = Visibility.Visible;

                Global.g_Main_ViewModel.LabelStatus = "当前用户：";
                Global.g_Main_ViewModel.LabelStatus += Global.g_User.UserName;
                Global.g_Main_ViewModel.LabelMessage = "";
            }
            else
            {
                Button_Cancel.Visibility = Visibility.Hidden;
                Button_Upload.Visibility = Visibility.Hidden;

                Global.g_Main_ViewModel.LabelStatus = "";
                Global.g_Main_ViewModel.LabelMessage = "";
            }

            Button_SignIn.Visibility = Visibility.Hidden;
        }
        //UserLogin command
        private void myCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            //Actions to perform 
            //Global.g_CameraList.Add(new Camera_Model() { Name = "From command", IsSelected = true });
            
            //Global.g_Main_ViewModel.BottomLabel = "当前用户：建设单位\r\n当前时间：";
            frmMain.NavigationService.Navigate(theLoginPage);

            myNavBarControl.ActiveGroup = navBarGroup_system;
            myNavBarControl.SelectedItem = navBarGroup_system.Items[0];

            Set_Button_Label(false);
        }
        private void myCommandExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            //Case of command's availability 
            if (true)
            {
                e.CanExecute = true;
            }
        }

        private void Button_Click_Upload(object sender, RoutedEventArgs e)
        {
            string sRet = "";
            
            if(Current_Menu_Item == Menu_Item.Inspect_Image_Upload)
            {
                sRet = db31.Inspect_Image_Upload();
            }
            else if (Current_Menu_Item == Menu_Item.Test_Image_Upload)
            {
                sRet = db31.Test_Image_Upload();
            }
           
            if(""!= sRet)
            {
                Global.g_Main_ViewModel.LabelStatus = sRet;
            }
        }
        private void Button_Click_Cancel(object sender, RoutedEventArgs e)
        {
            if(db31.WorkingStatus == DB31_Controller.Working_Status.Working)
            {
                db31.Stop_Uploading_Image = true;
            }
        }
        private void Button_Click_SignIn(object sender, RoutedEventArgs e)
        {

        }

        private void OnEvent_Login_Finished(object sender,string sRet)
        {
            //myNavBarControl.ActiveGroup = myNavBarControl.Groups[1];
            //myNavBarControl.SelectedItem = myNavBarControl.Groups[1].Items[0];

            switch(Global.g_User.Department)
            {
                case DB31_User.Enum_Department.Inspector:
                    navBarItem_Inspect_ImageUpload_Click(null, null);
                    break;
                case DB31_User.Enum_Department.Operator:
                    navBarItem_Test_ImageUpload_Click(null, null);
                    break;
                case DB31_User.Enum_Department.Maintainer:
                    navBarItem_Maintenance_Report_Click(null, null);
                    break;
            }
                
           
        }

        private void OnEvent_Working_Message(object sender, string sMsg)
        {
            Global.g_Main_ViewModel.LabelMessage = sMsg;
  
            Thread.Sleep(2000);
        }

        private void navBarItem41_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new FaultRepairMenu());
            Set_Button_Label(true);
        }
        private void navBarItem_Inspect_ImageUpload_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new SummaryTable());

            myNavBarControl.SelectedItem = navBarItem_Inspect_ImageUpload;

            Set_Button_Label(true);
            //Button_Cancel.IsEnabled = false;

            Current_Menu_Item = Menu_Item.Inspect_Image_Upload;
        }
        private void navBarItem_Test_ImageUpload_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new SummaryTable());

            myNavBarControl.SelectedItem = navBarItem_Test_ImageUpload;

            Set_Button_Label(true);
            //Button_Cancel.IsEnabled = false;

            Current_Menu_Item = Menu_Item.Test_Image_Upload;
        }

        private void navBarItem_Repair_SignIn_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(null);
            Set_Button_Label(true);
            Button_Upload.Visibility = Visibility.Hidden;
            Button_Cancel.Visibility = Visibility.Hidden;
            Button_SignIn.Visibility = Visibility.Visible;
        }
        private void navBarItem_Maintenance_Report_Click(object sender, EventArgs e)
        {
            myNavBarControl.SelectedItem = navBarItem_Maintenance_Report;
            frmMain.NavigationService.Navigate(new MaintenanceMenu());
            Set_Button_Label(true);

            Current_Menu_Item = Menu_Item.Maintenance_Report;
        }
        private void navBarItem_Repair_Report_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new RepairMenu());
            Set_Button_Label(true);
        }

    }
}
