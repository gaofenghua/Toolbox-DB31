using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toolbox_DB31.Classes;
using Toolbox_DB31.AVMS_Adapter;

namespace Toolbox_DB31.DB31_Adapter
{
    class DB31_Controller
    {
        enum OperationCmd_Type
        {
            Maintenance_Image_Upload = 2,
            Test_Image_Upload = 4,
            Requested_Image_Upload = 5,
            Repair_Upload = 29,
            Inspection_Image_Upload = 33
            
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

        public int DVR_State = 0;
        public long Total_Space = 0;
        public long Free_Space = 0;
        public string Process_Name = "System,AI_Main.exe";

        System.Threading.Timer heartbeat_timer = null;
        int Time_Interval = 60000 * 5;

        public DB31_Controller(DB31_User db31_user, string ip, int port)
        {
            user = db31_user;

            socket = new DB31_Socket(ip, port);
            socket.Working_Message += OnEvent_Receive_Socket_Message;
            socket.Data_Received += OnEvent_Socket_Data_Received;

            xml = new DB31_Xml();

            StartHeartbeat();
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
            GetStoredDiskSpace();

            string xml_content = xml.HeartbeatXml(DVR_State, Total_Space, Free_Space, Process_Name);
            Send(xml_content);
          
            heartbeat_timer.Change(Time_Interval, Timeout.Infinite);
        }

        private void Send_Message_Out(string sMsg)
        {
            if(null!= Working_Message)
            {
                Working_Message(this,sMsg);
            }
        }

        private void Upload_Image(OperationCmd_Type OpeType)
        {
            // Multithread notes:
            //dispatcher needed
            
            //Global.g_CameraList.Add(new Camera_Model() { AgentID = "11111111", ChannelNumber = 0, Name = "controler upload image", Status = "在线", IsSelected = true });

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

        public string Repair_Upload(string sNote)
        {
            //start form the information
            int Type = (int)OperationCmd_Type.Repair_Upload;
            int Channel = 0;
            DateTime TriggerTime = DateTime.Now;
            byte[] bNote = Encoding.UTF8.GetBytes(sNote);
            string Note = Convert.ToBase64String(bNote);
            string GUID = Guid.NewGuid().ToString();
            string base64image = "";
            string xml_content = xml.OperationCmd_Xml(Type, Channel, TriggerTime.ToString(), Note, GUID, base64image);

            new Task(x =>
            { Send((string)x); }, xml_content).Start();

            return "";
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
                int Type = (int)OpType;
                int Channel = cam.ChannelNumber;
                DateTime TriggerTime = DateTime.Now;
                byte[] bNote = Encoding.UTF8.GetBytes("图像上传");
                string Note = Convert.ToBase64String(bNote);
                GUID = GUID==null?Guid.NewGuid().ToString():GUID;
                //string base64image = Global.g_VMS_Adapter.GetEncodedSnapshot(cam.CameraID,TriggerTime,true);
                string base64image = "xxxxxxxxxxxxxxxxxxxx";
                string xml_content = xml.OperationCmd_Xml(Type, Channel, TriggerTime.ToString(), Note, GUID, base64image);

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

        private void GetStoredDiskSpace()
        {
            DeviceSummary.GetStoredDiskSpace("C://", out Total_Space, out Free_Space);
        }
    }
}
