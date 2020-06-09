using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toolbox_DB31.Classes;
using Toolbox_DB31.AVMS_Adapter;
using System.IO;
using System.Diagnostics;
using System.ServiceProcess;

namespace Toolbox_DB31.DB31_Adapter
{
    public class DB31_Controller
    {
        enum OperationCmd_Type
        {
            Alarm_Image_Upload = 1,
            Maintenance_Image_Upload = 2,
            Daily_Image_Upload = 3,
            Test_Image_Upload = 4,
            Requested_Image_Upload = 5,
            Video_Surveillance_Error = 15,
            DVR_Start = 19,
            DVR_Exit = 20,
            DVR_Abnormal_Quit = 21,
            DVR_Parameter_Set = 22,
            DVR_Parameter_Save = 23,
            DVR_Video_Lost = 24,
            DVR_Motion_Detect = 25,
            DVR_External_Trigger = 26,
            System_Alarm_Restore = 27,
            DVR_Illegal_Exit = 28,
            Repair_Report = 29,
            Maintenance_Report = 30,
            DVR_Local_Playback = 31,
            Inspection_Image_Upload = 33,
            Alarm_Image_Upload_No_Recording = 36,
            Maintenance_Image_Upload_No_Recording = 37,
            Daily_Image_Upload_No_Recording = 38,
            Test_Image_Upload_No_Recording = 39,
            Inspection_Image_Upload_No_Recording = 40,
            DVR_Disk_Error = 41
        };

        public enum Working_Status { Available, Working };

        public Mutex Status_Mutex = new Mutex();
        public Working_Status WorkingStatus = Working_Status.Available;
        public bool Stop_Uploading_Image = false;

        public event Action<object, string> Working_Message;
        private string sMsg;

        DB31_User user = null;

        DB31_Socket socket;
        DB31_Xml xml;

        public string Default_AgentID = "000000000000"; //Should be 12 characters
        public int DVR_State = 0;
        public long Total_Space = 0;
        public long Free_Space = 0;
        public string Process_Name = "AI InfoService";
        public ServiceControllerStatus Current_Process_Status = ServiceControllerStatus.Stopped;

        public int Seconds_Before_Alarm = 5;
        public int Seconds_After_Alarm = 50;
        public int Alarm_Interval = 20;

        System.Threading.Timer heartbeat_timer = null;
        int Time_Interval = 60000 * 5;

        System.Threading.Timer self_check_timer = null;
        int Self_Check_Interval = 1000 * 5;

        private string Temp_File = System.Windows.Forms.Application.StartupPath.ToString() + @"\" + "toolbox.tmp";

        public DB31_Controller(DB31_User db31_user, string ip, int port)
        {
            Get_Default_AgentID();

            user = db31_user;

            socket = new DB31_Socket(ip, port);
            socket.Working_Message += OnEvent_Receive_Socket_Message;
            socket.Data_Received += OnEvent_Socket_Data_Received;

            xml = new DB31_Xml();

        }

        ~DB31_Controller()
        {
            File.Delete(Temp_File);
        }

        public void Start()
        {
            //Need Global.g_VMS_Adapter to be ready

            StartHeartbeat();

            StartSelfCheck();

            if (true == Crash_Last_Time())
            {
                //report crash event
                DVRAbnormalQuit();
                DVR_Illegal_Exit_Upload();
            }
            Write_Temp_File();
        }
        private void OnEvent_Receive_Socket_Message(object sender, SocketWorkingEventArgs e)
        {
            if(e.CurrentStatus == DB31_Socket.Status.Connected && e.CurrentStatus != e.PreviousStatus)
            {
                //StartHeartbeat();
            }

            Send_Message_Out(e.sMessage);
        }

        private void OnEvent_Socket_Data_Received(object sender, string sXml)
        {
            Xml_Parse_Output xInfo = xml.ParseXml(sXml);

            if(xInfo.Ticks > 0)
            {
                //Time_Interval = xInfo.Ticks * 60000; //Ticks：单位分钟
                //StartHeartbeat();

                //Receive GetImage command
                if(xInfo.Channel != null)
                {
                    Respond_To_GetImage(xInfo.Channel,xInfo.GUID);
                }
            }

            if(xInfo.OK_NowTime != null)
            {
                socket.Close();

                Status_Mutex.WaitOne();

                WorkingStatus = Working_Status.Available;

                Status_Mutex.ReleaseMutex();
            }

        }

        public void StartHeartbeat()
        {
            if(heartbeat_timer == null)
            {
                heartbeat_timer = new Timer(HeartBeat, null, 0, Timeout.Infinite);
            }
            else 
            {
                heartbeat_timer.Change(0, Timeout.Infinite);
            }

            sMsg = Time_Interval / 1000 + "秒后发送心跳信息。";
            Send_Message_Out(sMsg);
        }

        public void HeartBeat(object obj)
        {
            //Write_Temp_File();

            GetStoredDiskSpace();

            string sAgent = GetAgentID();

            string xml_content = xml.HeartbeatXml(sAgent,DVR_State, Total_Space, Free_Space, Process_Name);
            Send(xml_content);

            string sMsg = "Heartbeat: DVR_State = " + DVR_State;
            Global.WriteLog(sMsg);

            heartbeat_timer.Change(Time_Interval, Timeout.Infinite);
        }

        private void Send_Message_Out(string sMsg)
        {
            if(null!= Working_Message)
            {
                Working_Message(this,sMsg);
            }
        }

        private void StartSelfCheck()
        {
            if (self_check_timer == null)
            {
                self_check_timer = new Timer(SelfCheck, null, 0, Timeout.Infinite);
            }
            else
            {
                self_check_timer.Change(0, Timeout.Infinite);
            }
        }

        private void SelfCheck(object obj)
        {
            var serviceControllers = ServiceController.GetServices();
            var server = serviceControllers.FirstOrDefault(service => service.ServiceName == Process_Name);
            if (server != null && server.Status != Current_Process_Status)
            {
                string sMsg = "Process status changed, " + Current_Process_Status.ToString() + " -> " + server.Status.ToString();
                Global.WriteLog(sMsg);

                if(server.Status == ServiceControllerStatus.Running)
                {
                    DVRStarted();
                }
                else if(server.Status == ServiceControllerStatus.Stopped)
                {
                    DVRExit();
                }

                Current_Process_Status = server.Status;
            }

            //start next time
            self_check_timer.Change(Self_Check_Interval, Timeout.Infinite);
        }
        private void Upload_Image(OperationCmd_Type OpeType)
        {
            //Copy all data to local
            Global.g_CameraList_Mutex.WaitOne();

            ArrayList cameras = new ArrayList();
            foreach(Camera_Model cam in Global.g_CameraList)
            {
                if(true == cam.IsSelected)
                {
                    cameras.Add(cam.Clone());
                }
            }
     
            Global.g_CameraList_Mutex.ReleaseMutex();
            // Finish copy

            Send_Image(OpeType, cameras, null);

        }

        public string Daily_Image_Upload()
        {
            new Task(x =>
            { Upload_Image((OperationCmd_Type)x); }, OperationCmd_Type.Daily_Image_Upload).Start();

            return "";
        }
        public string Inspect_Image_Upload()
        {
            if (true == user.Privilege_Check(DB31_User.Enum_Action.Inspect_Image_Upload))
            {
                new Task(x=>
                { Upload_Image((OperationCmd_Type)x); },OperationCmd_Type.Inspection_Image_Upload).Start();

                return "";
            }
            return "没有操作权限！";
        }
        public string Maintenance_Image_Upload()
        {
            if (true == user.Privilege_Check(DB31_User.Enum_Action.Maintenance_Image_Upload))
            {
                new Task(x =>
                { Upload_Image((OperationCmd_Type)x); }, OperationCmd_Type.Maintenance_Image_Upload).Start();

                return "";
            }
            return "没有操作权限！";
        }
        public string Test_Image_Upload()
        {
            if (true == user.Privilege_Check(DB31_User.Enum_Action.Test_Image_Upload))
            {
                new Task(x =>
                { Upload_Image((OperationCmd_Type)x); }, OperationCmd_Type.Test_Image_Upload).Start();

                return "";
            }
            return "没有操作权限！";
        }

        public string Maintenance_Upload(string sNote)
        {
            int Type = (int)OperationCmd_Type.Maintenance_Report;

            Note_Upload(Type, sNote,DateTime.Now);

            return "";
        }
        public string Repair_Upload(string sNote)
        {
            int Type = (int)OperationCmd_Type.Video_Surveillance_Error;

            Note_Upload(Type, sNote,DateTime.Now);

            return "";
        }

        public string Sign_In()
        {
            int Type = (int)OperationCmd_Type.Maintenance_Report;
            Note_Upload(Type, "系统维保签到",DateTime.Now);

            Type = (int)OperationCmd_Type.Repair_Report;
            Note_Upload(Type, "系统维修签到",DateTime.Now);

            return "";
        }

        public string Disk_Error_Upload()
        {
            int Type = (int)OperationCmd_Type.DVR_Disk_Error;
            Note_Upload(Type, "DVR磁盘错误",DateTime.Now);

            return "";
        }

        public string DVR_Video_Lost_Upload(DateTime AlarmTime)
        {
            int Type = (int)OperationCmd_Type.DVR_Video_Lost;
            Note_Upload(Type, "DVR视频丢失",AlarmTime);

            return "";
        }

        public string DVR_Motion_Detect_Upload(DateTime AlarmTime)
        {
            int Type = (int)OperationCmd_Type.DVR_Motion_Detect;
            Note_Upload(Type, "DVR移动侦测",AlarmTime);

            return "";
        }

        public string DVR_External_Trigger_Upload(DateTime AlarmTime)
        {
            int Type = (int)OperationCmd_Type.DVR_External_Trigger;
            Note_Upload(Type, "DVR外部触发",AlarmTime);

            return "";
        }

        public string System_Alarm_Restore_Upload(DateTime AlarmTime)
        {
            int Type = (int)OperationCmd_Type.System_Alarm_Restore;
            Note_Upload(Type, "系统报警恢复",AlarmTime);

            return "";
        }

        public string DVR_Local_Playback_Upload()
        {
            int Type = (int)OperationCmd_Type.DVR_Local_Playback;
            Note_Upload(Type, "DVR本地回放",DateTime.Now);

            return "";
        }

        public string DVR_Parameter_Set_Upload()
        {
            int Type = (int)OperationCmd_Type.DVR_Parameter_Set;
            Note_Upload(Type, "DVR参数设置",DateTime.Now);

            return "";
        }

        public string DVR_Parameter_Save_Upload()
        {
            int Type = (int)OperationCmd_Type.DVR_Parameter_Save;
            Note_Upload(Type, "DVR参数保存",DateTime.Now);

            return "";
        }

        public string DVR_Illegal_Exit_Upload()
        {
            int Type = (int)OperationCmd_Type.DVR_Illegal_Exit;
            Note_Upload(Type, "DVR非法退出",DateTime.Now);

            return "";
        }
        private void Note_Upload(int iType, string sNote, DateTime AlarmTime)
        {
            //start form the information
            string sAgent = GetAgentID();
            int Channel = 0;
            DateTime TriggerTime = AlarmTime;
            byte[] bNote = Encoding.UTF8.GetBytes(sNote);
            string Note = Convert.ToBase64String(bNote);
            string GUID = Guid.NewGuid().ToString();
            string base64image = "";
            string xml_content = xml.OperationCmd_Xml(sAgent, iType, Channel, TriggerTime.ToString(), Note, GUID, base64image);

            new Task(x =>
            { Send((string)x); }, xml_content).Start();
        }

        private void Respond_To_GetImage(string sChannel, string sGUID)
        {
            string[] channelArray = sChannel.Split(',');
            int count = channelArray.Length;

            ArrayList cameras = new ArrayList();

            //Find the camera
            foreach(string s_channel in channelArray)
            {
                int n_channel = -1;
                int.TryParse(s_channel, out n_channel);

                Global.g_CameraList_Mutex.WaitOne();

                Camera_Model cam = Find_Camera_From_ChannelNumber(n_channel);
                if(null != cam)
                {
                    cameras.Add(cam.Clone());
                }

                Global.g_CameraList_Mutex.ReleaseMutex();
            }

            //Send image
            Send_Image(OperationCmd_Type.Requested_Image_Upload, cameras, sGUID);
        }

        private Camera_Model Find_Camera_From_ChannelNumber(int nChannelNumber)
        {
            foreach (Camera_Model cam in Global.g_CameraList)
            {
                if (cam.ChannelNumber == nChannelNumber)
                {
                    return cam;
                }
            }
            return null;
        }
        private Camera_Model Find_Camera_From_CameraID(int nCameraID)
        {
            foreach (Camera_Model cam in Global.g_CameraList)
            {
                if (cam.CameraID == nCameraID)
                {
                    return cam;
                }
            }
            return null;
        }
        private void Send_Image(OperationCmd_Type OpType, ArrayList Cameras, string GUID)
        {
            string messageHead="",messageContent="";
            if(OpType == OperationCmd_Type.Inspection_Image_Upload)
            {
                messageHead = "验收：";
            }
            else if (OpType == OperationCmd_Type.Maintenance_Image_Upload)
            {
                messageHead = "维保：";
            }
            else if (OpType == OperationCmd_Type.Test_Image_Upload)
            {
                messageHead = "测试：";
            }
            else if (OpType == OperationCmd_Type.Daily_Image_Upload)
            {
                messageHead = "日常：";
            }
            else if (OpType == OperationCmd_Type.Requested_Image_Upload)
            {
                messageHead = "按需上传：";
            }
       

            foreach (Camera_Model cam in Cameras)
            {
                if (true == Stop_Uploading_Image)
                {
                    messageContent = "停止上传图像";
                    Send_Message_Out(messageHead+messageContent);

                    Stop_Uploading_Image = false;
                    
                    return;
                }

                //start form the information
                string sAgent = cam.AgentID;
                int Type = (int)OpType;
                int Channel = cam.ChannelNumber;
                DateTime TriggerTime = DateTime.Now;
                byte[] bNote = Encoding.UTF8.GetBytes("图像上传 " + Global.g_User.UserDisplayName);
                string Note = Convert.ToBase64String(bNote);
                GUID = GUID==null?Guid.NewGuid().ToString():GUID;

                string base64image = "";
                if (Global.g_VMS_Adapter.GetCameraStateById(cam.CameraID) == SeerInterfaces.EStates.NoRecord)
                {
                    if (OpType == OperationCmd_Type.Daily_Image_Upload)
                    {
                        Type = (int)OperationCmd_Type.Daily_Image_Upload_No_Recording;
                    }
                    else if (OpType == OperationCmd_Type.Inspection_Image_Upload)
                    {
                        Type = (int)OperationCmd_Type.Inspection_Image_Upload_No_Recording;
                    }
                    else if (OpType == OperationCmd_Type.Maintenance_Image_Upload)
                    {
                        Type = (int)OperationCmd_Type.Maintenance_Image_Upload_No_Recording;
                    }
                    else if (OpType == OperationCmd_Type.Test_Image_Upload)
                    {
                        Type = (int)OperationCmd_Type.Test_Image_Upload_No_Recording;
                    }
                }
                else
                {
                    base64image = Global.g_VMS_Adapter.GetEncodedSnapshot(cam.CameraID, TriggerTime, false);
                }

                string xml_content = xml.OperationCmd_Xml(sAgent,Type, Channel, TriggerTime.ToString(), Note, GUID, base64image);

                //Message to the main frame
                if (null != Working_Message)
                {
                    messageContent = "正在上传图像 ";
                    messageContent += cam.Name;

                    Send_Message_Out(messageHead+messageContent);
                }

                Send(xml_content);
            }
            messageContent = "操作完成。";
            Send_Message_Out(messageHead+messageContent);
        }

        private bool Send(string xml_content)
        {
            bool bGetAvailable = false;
            for (int i = 0; i < 10;i++ )
            {
                Status_Mutex.WaitOne();
                if(WorkingStatus == Working_Status.Working)
                {
                    Status_Mutex.ReleaseMutex();   
                    Thread.Sleep(1000);
                }
                else
                {
                    WorkingStatus = Working_Status.Working;
                    Status_Mutex.ReleaseMutex();   
                    bGetAvailable = true;

                    break;
                }
            }

            if(bGetAvailable == false)
            {
                string messageContent = "发送超时。";
                Send_Message_Out(messageContent);
                return false;
            }

            bGetAvailable = false;

            socket.ReConnect();
            bool bRet = socket.Send(xml_content);

            if (true == bRet)
            {
                // Waiting for return message "OK_NowTime"
                for (int i = 0; i < 10; i++)
                {
                    Status_Mutex.WaitOne();
                    if (WorkingStatus == Working_Status.Working)
                    {
                        Status_Mutex.ReleaseMutex();
                        Thread.Sleep(1000);
                    }
                    else
                    {
                        Status_Mutex.ReleaseMutex();
                        bGetAvailable = true;

                        break;
                    }
                }
            }

            if(bGetAvailable == false)
            {
                socket.Close();

                Status_Mutex.WaitOne();

                WorkingStatus = Working_Status.Available;
                
                Status_Mutex.ReleaseMutex();
            }

            return true;
        }

        public void Alarm_Image_Upload(int CameraID,DateTime AlarmTime)
        {
            //Check the alarm enable flag
            Camera_Model cam = Find_Camera_From_CameraID(CameraID);
            if (null == cam || cam.AlarmEnable == false)
            {
                return;
            }

            //start form the information
            string sAgent = cam.AgentID;
            int Type = (int)OperationCmd_Type.Alarm_Image_Upload;
            int Channel = cam.ChannelNumber;
            DateTime TriggerTime = AlarmTime;
            byte[] bNote = Encoding.UTF8.GetBytes("报警图像上传");
            string Note = Convert.ToBase64String(bNote);
            string GUID = Guid.NewGuid().ToString();
            string base64image = "";
            string xml_content = xml.OperationCmd_Xml(sAgent, Type, Channel, TriggerTime.ToString(), Note, GUID, base64image);

            string sMessage = "通道" + Channel +":报警图像上传";
            //Message to the main frame
            if (null != Working_Message)
            {
                Send_Message_Out(sMessage);
            }
            Global.WriteLog(sMessage);

            Send(xml_content);

            if(Alarm_Interval == 0)
            {
                return;
            }

            int nCount = (Seconds_After_Alarm + Seconds_Before_Alarm) / Alarm_Interval + 1;
            DateTime ImageTime; 
            for(int i=0;i<nCount;i++)
            {
                ImageTime = TriggerTime.AddSeconds(-Seconds_Before_Alarm + (Alarm_Interval*i));

                TimeSpan span = ImageTime.Subtract(DateTime.Now);
                
                int Seconds_to_Future = span.Seconds;
                //Trace.WriteLine("\r\n=================================" + Seconds_to_Future.ToString());
                if(Seconds_to_Future > 0)
                {
                    Thread.Sleep((Seconds_to_Future + 1) * 1000);
                }

                base64image = Global.g_VMS_Adapter.GetEncodedSnapshot(Channel, ImageTime, false);
                xml_content = xml.OperationCmd_Xml(sAgent, Type, Channel, ImageTime.ToString(), Note, GUID, base64image);
                Send(xml_content);

                //Message to the main frame
                if (null != Working_Message)
                {
                    Send_Message_Out(sMessage + (i + 1) + "/" + nCount);
                }
                Global.WriteLog(sMessage + (i + 1) + "/" + nCount);
            }
        }

        private void DVRStarted()
        {
            //start form the information
            string sAgent = GetAgentID();
            int Type = (int)OperationCmd_Type.DVR_Start;
            int Channel = 0;
            DateTime TriggerTime = DateTime.Now;
            byte[] bNote = Encoding.UTF8.GetBytes("DVR系统启动");
            string Note = Convert.ToBase64String(bNote);
            string GUID = Guid.NewGuid().ToString();
            string base64image = "";
            string xml_content = xml.OperationCmd_Xml(sAgent, Type, Channel, TriggerTime.ToString(), Note, GUID, base64image);

            Send(xml_content);
        }

        private void DVRExit()
        {
            //start form the information
            string sAgent = GetAgentID();
            int Type = (int)OperationCmd_Type.DVR_Exit;
            int Channel = 0;
            DateTime TriggerTime = DateTime.Now;
            byte[] bNote = Encoding.UTF8.GetBytes("DVR系统退出");
            string Note = Convert.ToBase64String(bNote);
            string GUID = Guid.NewGuid().ToString();
            string base64image = "";
            string xml_content = xml.OperationCmd_Xml(sAgent, Type, Channel, TriggerTime.ToString(), Note, GUID, base64image);

            Send(xml_content);
        }

        private void DVRAbnormalQuit()
        {
            //start form the information
            string sAgent = GetAgentID();
            int Type = (int)OperationCmd_Type.DVR_Abnormal_Quit;
            int Channel = 0;
            DateTime TriggerTime = DateTime.Now;
            byte[] bNote = Encoding.UTF8.GetBytes("DVR异常退出");
            string Note = Convert.ToBase64String(bNote);
            string GUID = Guid.NewGuid().ToString();
            string base64image = "";
            string xml_content = xml.OperationCmd_Xml(sAgent, Type, Channel, TriggerTime.ToString(), Note, GUID, base64image);

            Send(xml_content);
        }
        private void GetStoredDiskSpace()
        {
            //AVMS adapter :GetStoredPath
            // GetStoredPath will return null at the first time
            //
            string storedPath = Global.g_VMS_Adapter.GetStoredPath();
            storedPath = storedPath==null?"C:\\": storedPath;
            DeviceSummary.GetStoredDiskSpace(storedPath, out Total_Space, out Free_Space);
        }

        private bool Crash_Last_Time()
        {
            string sFileName = Temp_File;

            if (File.Exists(sFileName))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void Write_Temp_File()
        {
            string path = Temp_File;
            bool isAppend = true;

            using (StreamWriter sw = new StreamWriter(path, isAppend, System.Text.Encoding.UTF8))
            {
                string cont = DateTime.Now + "Controller heartbeat.";
                sw.WriteLine(cont);
                sw.Flush();
                sw.Close();
            }
        }

        public void Delete_Temp_File()
        {
            File.Delete(Temp_File);
        }
        private string GetAgentID()
        {
            return Default_AgentID;
        }

        private void Get_Default_AgentID()
        {
            string firstLine = null;

            string file = System.Windows.Forms.Application.StartupPath.ToString() + @"\" + "Configuration.csv";
            if (File.Exists(file) == false)
            {
                return;
            }

            try
            {
                using (StreamReader sr = new StreamReader(file))
                {
                    firstLine = sr.ReadLine();
                    sr.Close();
                }
            }
            catch (Exception e)
            {
                Global.WriteLog("Error read file " + file + " " + e.Message);
            }
            
            if(firstLine == null)
            {
                return;
            }

            string[] info = firstLine.Trim().Split(',');
            if(info.Length < 1)
            {
                return;
            }

            Default_AgentID = info[0];

        }
    }
}
