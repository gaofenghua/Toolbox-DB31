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
using System.IO;
using System.Xml.Linq;
using System.Timers;

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DevExpress.Xpf.Core.ThemedWindow
    {
        public static RoutedCommand myCommand = new RoutedCommand();

        LoginPage theLoginPage;

        enum Menu_Item { User_Login, Inspect_Image_Upload, Maintenance_Image_Upload, Test_Image_Upload, Maintenance_Report, Repair_Report,Setting_Menu,Repair_SignIn,Fault_Repair_Report};
        Menu_Item Current_Menu_Item;

        DB31_Controller db31;
        string DB31_IP = null;
        int DB31_Port = 0;

        string AVMS_IP = null;
        string AVMS_UserName = null;
        string AVMS_Password = null;

        private System.Timers.Timer m_timer = null;
        private SettingsMenu m_settings = null;
        private DateTime m_LastUploadDT = new DateTime();

        public MainWindow()
        {
            InitializeComponent();

            ReadUserConfigurationFile();

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

            db31 = new DB31_Controller(Global.g_User, DB31_IP,DB31_Port);
            db31.Working_Message += OnEvent_Working_Message;
            Global.g_DB31_Adapter = db31;

            m_settings = new SettingsMenu(this);
            m_timer = new System.Timers.Timer(50 * 1000);
            m_timer.Elapsed += DailyUpload;
            SetDailyTimer(m_settings.m_ViewModel.IsDailyTimerEnabled);

            DeviceSummary.CfgFilePath = @".\Configuration.csv";
            AVMSAdapter adapter = new AVMSAdapter();
            adapter.Start(AVMS_IP, AVMS_UserName, AVMS_Password); 
            adapter.AVMSTriggered += new AVMSAdapter.AVMSTriggeredHandler(HandleAVMSEvent);

            GeneralStorageManager generalStorage = new GeneralStorageManager();
            generalStorage.NotificationReceived += new EventHandler<StorageEventArgs>(HandleStorageEvent);

            Global.g_VMS_Adapter = adapter;
            Global.g_Storage_List = new List<StorageManager>();
            Global.g_Storage_List.Add(generalStorage);

            db31.Start();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            db31.DVR_State = 1;
            db31.HeartBeat(null);
            db31.Delete_Temp_File();

            if (0 != Global.g_Storage_List.Count)
            {
                foreach (StorageManager storage in Global.g_Storage_List)
                {
                    storage.NotificationReceived -= new EventHandler<StorageEventArgs>(HandleStorageEvent);
                    storage.Dispose();
                }
            }
            Global.g_Storage_List = null;

            Global.g_VMS_Adapter.AVMSTriggered -= new AVMSAdapter.AVMSTriggeredHandler(HandleAVMSEvent);
            Global.g_VMS_Adapter.Stop();
            Global.g_VMS_Adapter = null;

            base.OnClosing(e);
        }
        public void SetDailyTimer(bool isEnabled)
        {
            m_timer.Enabled = isEnabled;
        }

        private void DailyUpload(object sender, ElapsedEventArgs e)
        {
            if (null == m_settings)
            {
                return;
            }

            DateTime checkDT = e.SignalTime;
            DateTime updateDT = m_settings.m_ViewModel.DailyUpdateDateTime;
            if (!IsEqualDT(m_LastUploadDT, checkDT) && IsEqualDT(updateDT, checkDT))
            {
                UploadInstantImage();
                m_LastUploadDT = checkDT;
            }
        }

        private bool IsEqualDT(DateTime cVal, DateTime bVal)
        {
            return (bVal.Hour == cVal.Hour) && (bVal.Minute == cVal.Minute);
        }

        private void UploadInstantImage()
        {
            string status = db31.Daily_Image_Upload();
            if (string.Empty != status)
            {
                //
            }
        }

        private void ReadUserConfigurationFile()
        {
            string file = System.Windows.Forms.Application.StartupPath.ToString() + @"\" + "toolbox.conf";
            if (File.Exists(file) == false)
            {
                return;
            }

            XDocument xd;
            int nQuery = 0;
            XElement item;

            try
            {
                xd = XDocument.Load(file);
            }
            catch (Exception e)
            {
                return;
            }

            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "ip" && s.Parent.Name.LocalName == "DB31"
                        select s;

            nQuery = query.Count();
            if (nQuery == 1)
            {
                item = query.First();
                DB31_IP = item.Value;
            }

            query = from s in xd.Descendants()
                        where s.Name.LocalName == "port" && s.Parent.Name.LocalName == "DB31"
                        select s;

            nQuery = query.Count();
            if (nQuery == 1)
            {
                item = query.First();
                string sPort = item.Value;
                int.TryParse(sPort, out DB31_Port);
            }

            // AVMS server info
            query = from s in xd.Descendants()
                    where s.Name.LocalName == "Server" && s.Parent.Name.LocalName == "AVMS"
                    select s;

            nQuery = query.Count();
            if (nQuery == 1)
            {
                item = query.First();
                AVMS_IP = item.Value;
                AVMS_UserName = item.Attribute("UserName") == null ? "admin" : item.Attribute("UserName").Value;
                AVMS_Password = item.Attribute("Password") == null ? "admin" : item.Attribute("Password").Value;
            }
        }

        private void HandleAVMSEvent(object sender, AVMSEventArgs e)
        {
            AVMS_ALARM alarmType = e.m_alarmType;
            DateTime alarmTime = e.m_alarmTime;
            int channelId = -1;
            int.TryParse(e.m_cameraId.ToString(), out channelId);
            string picData = e.m_pictureData;

            if (AVMS_ALARM.AVMS_ALARM_CONNECTED == alarmType)
            {
                ((AVMSAdapter)sender).ShowCameraList();
            }
            else if (AVMS_ALARM.AVMS_ALARM_CONNECTIONLOST == alarmType)
            {
                ((AVMSAdapter)sender).HideCameraList();
            }
            else if (AVMS_ALARM.AVMS_ALARM_DEVICERESTORE == alarmType)
            {
                App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                {
                    DeviceSummary.UpdateDeviceState(e.m_cameraId, true);
                    if (Current_Menu_Item == Menu_Item.Inspect_Image_Upload || Current_Menu_Item == Menu_Item.Maintenance_Image_Upload || Current_Menu_Item == Menu_Item.Test_Image_Upload)
                    {
                        SummaryTable summaryTable = (SummaryTable)frmMain.Content;
                        summaryTable.GridControl_Summary.RefreshData();
                    }
                });
            }
            else if (alarmType == AVMS_ALARM.AVMS_ALARM_DEVICELOST)
            {
                //Find the camera
                foreach (Camera_Model cam in Global.g_CameraList)
                {
                    if (cam.CameraID == e.m_cameraId)
                    {
                        cam.Status = "离线";
                        break;
                    }
                }

                //Update GUI
                if(Current_Menu_Item == Menu_Item.Inspect_Image_Upload || Current_Menu_Item == Menu_Item.Maintenance_Image_Upload || Current_Menu_Item == Menu_Item.Test_Image_Upload)
                {
                    //Dispatcher needed
                    //SummaryTable summaryTable = (SummaryTable)frmMain.Content;
                    //summaryTable.GridControl_Summary.RefreshData();

                    App.Current.Dispatcher.BeginInvoke((Action)delegate ()
                    {
                        SummaryTable summaryTable = (SummaryTable)frmMain.Content;
                        summaryTable.GridControl_Summary.RefreshData();
                    });
                }
            }
            else if (alarmType == AVMS_ALARM.AVMS_ALARM_VIDEOLOSS)
            {
                //Send alarm
                db31.DVR_Video_Lost_Upload(alarmTime);

                db31.Alarm_Image_Upload(channelId, alarmTime);
            }
            else if (alarmType == AVMS_ALARM.AVMS_ALARM_VMD)
            {
                db31.DVR_Motion_Detect_Upload(alarmTime);

                db31.Alarm_Image_Upload(channelId, alarmTime);
            }
            else if (alarmType == AVMS_ALARM.AVMS_ALARM_HARDWARETRIGGER)
            {
                db31.DVR_External_Trigger_Upload(alarmTime);

                db31.Alarm_Image_Upload(channelId, alarmTime);
            }
            else if (alarmType == AVMS_ALARM.AVMS_ALARM_RESTORE)
            {
                db31.System_Alarm_Restore_Upload(alarmTime);

                db31.Alarm_Image_Upload(channelId, alarmTime);
            }
            else
            {
                db31.Alarm_Image_Upload(channelId, alarmTime);
            }
            
        }

        private void HandleStorageEvent(object sender, StorageEventArgs e)
        {
            STORAGE_MANUFACTURER storageOwner = e.StorageOwner;
            STORAGE_EVENT storageEvent = e.StorageEvent;
            StoragePropertyStruct storageProperties = e.StorageProperties;

            if(storageEvent == STORAGE_EVENT.VOLUME_NEW_APPENDED || storageEvent == STORAGE_EVENT.VOLUME_OLD_REMOVED)
            {
                db31.Disk_Error_Upload();
            }
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

            Current_Menu_Item = Menu_Item.User_Login;
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
            else if (Current_Menu_Item == Menu_Item.Maintenance_Image_Upload)
            {
                sRet = db31.Maintenance_Image_Upload();
            }
            else if (Current_Menu_Item == Menu_Item.Maintenance_Report)
            {
                MaintenanceMenu maintenancePage = (MaintenanceMenu)frmMain.Content;
                string sNote = maintenancePage.Get_Notes();

                sRet = db31.Maintenance_Upload(sNote);
            }
            else if (Current_Menu_Item == Menu_Item.Fault_Repair_Report)
            {
                FaultRepairMenu faultRepairMenu = (FaultRepairMenu)frmMain.Content;
                string sNote = faultRepairMenu.Get_Notes();

                sRet = db31.Repair_Upload(sNote);
            }
            else if (Current_Menu_Item == Menu_Item.Repair_Report)
            {
                RepairMenu repairPage = (RepairMenu) frmMain.Content;

                string sNote = "系统维修：";
                if(repairPage.m_ViewModel.IsVideoMonitorEnabled)
                {
                    sNote += "视频：";
                }

                sNote += repairPage.m_ViewModel.RepairRecords;

                if(repairPage.m_ViewModel.m_Status==RepairStatus.NOT_REPAIR)
                {
                    sNote += "--未维修";
                }
                else if(repairPage.m_ViewModel.m_Status == RepairStatus.PART_REPAIR)
                {
                    sNote += "--部分维修";
                }
                else if (repairPage.m_ViewModel.m_Status == RepairStatus.TOTAL_REPAIR)
                {
                    sNote += "--完全维修";
                }

                sRet = db31.Repair_Upload(sNote);
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

            //some manual test
            //AVMSEventArgs EV = new AVMSEventArgs(AVMS_ALARM.AVMS_ALARM_DEVICELOST, DateTime.Now, 0, null);
            //HandleAVMSEvent(null,EV);
            
            //finished manual test
        }
        private void Button_Click_SignIn(object sender, RoutedEventArgs e)
        {
            db31.Sign_In();
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
            Current_Menu_Item = Menu_Item.Fault_Repair_Report;
        }
        private void navBarItem_Inspect_ImageUpload_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new SummaryTable());

            myNavBarControl.SelectedItem = navBarItem_Inspect_ImageUpload;

            Set_Button_Label(true);
            //Button_Cancel.IsEnabled = false;

            Current_Menu_Item = Menu_Item.Inspect_Image_Upload;
        }

        private void navBarItem_Maintenance_ImageUpload_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(new SummaryTable());

            myNavBarControl.SelectedItem = navBarItem_Maintenance_ImageUpload;

            Set_Button_Label(true);
           
            Current_Menu_Item = Menu_Item.Maintenance_Image_Upload;
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

            Current_Menu_Item = Menu_Item.Repair_SignIn;
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

            Current_Menu_Item = Menu_Item.Repair_Report;
        }

        private void navBarItem_Settings_Page_Click(object sender, EventArgs e)
        {
            frmMain.NavigationService.Navigate(m_settings);
            Set_Button_Label(false);

            Current_Menu_Item = Menu_Item.Setting_Menu;
        }

        public void UploadAlarmLog(int camId, string log)
        {
            // db31 method
        }

        public void UploadEventLog(int camId, string log)
        {
            new Task(x =>
            { ParseAndSendEventLog((string)x); }, log).Start();
        }

        private void ParseAndSendEventLog(string log)
        {
            // db31 method

            //参数设置：ConfigurationChange 参数设置保存/进入设置状态
            //录像回放：CameraHistoryConnect、CameraHistoryDisconnect 录像回放操作

            if (null == log)
            {
                return;
            }
            StringReader sLog = new StringReader(log);

            DateTime EventTime;
            string sLine = "";
            int iFirst = -1;
            while ((sLine = sLog.ReadLine()) != null)
            {
                iFirst = sLine.IndexOf("ConfigurationChange");
                if (iFirst > 0)
                {
                    EventTime = GetEventTime(sLine);
                    iFirst = sLine.IndexOf("username");
                    if (iFirst > 0)
                    {
                        db31.DVR_Parameter_Set_Upload(EventTime);
                    }
                    else
                    {
                        db31.DVR_Parameter_Save_Upload(EventTime);
                    }
                    Thread.Sleep(5000);
                    continue;
                }

                iFirst = sLine.IndexOf("CameraHistoryConnect");
                if (iFirst > 0)
                {
                    EventTime = GetEventTime(sLine);
                    db31.DVR_Local_Playback_Upload(EventTime);
                    Thread.Sleep(5000);
                    continue;
                }
            }
        }

        DateTime GetEventTime(string sLine)
        {
            DateTime EventTime = DateTime.Now;

            string[] info = sLine.Trim().Split(',');
            if (info.Length >= 1)
            {
                DateTime.TryParse(info[0], out EventTime);
            }

            return EventTime;
        }
    }
}
