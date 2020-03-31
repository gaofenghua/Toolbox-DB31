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
            Test_Image_Upload = 4,
            Requested_Image_Upload = 5,
            Inspection_Image_Upload = 33
            
        };

        public enum Working_Status { Available, Working };
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
        int Time_Interval = 1000 * 5;

        public DB31_Controller(DB31_User db31_user)
        {
            user = db31_user;

            socket = new DB31_Socket();
            socket.Working_Message += OnEvent_Receive_Socket_Message;
            socket.Data_Received += OnEvent_Socket_Data_Received;

            xml = new DB31_Xml();

            
        }

        private void OnEvent_Receive_Socket_Message(object sender, SocketWorkingEventArgs e)
        {
            if(e.CurrentStatus == DB31_Socket.Status.Connected && e.CurrentStatus != e.PreviousStatus)
            {
                StartHeartbeat();
            }

            Send_Message_Out(e.sMessage);
        }

        private void OnEvent_Socket_Data_Received(object sender, string sXml)
        {
            Xml_Parse_Output xInfo = xml.ParseXml(sXml);

            if(xInfo.Ticks > 0)
            {
                Time_Interval = xInfo.Ticks * 60000; //Ticks：单位分钟
                StartHeartbeat();

                //Receive GetImage command
                if(xInfo.Channel != null)
                {
                    Respond_To_GetImage(xInfo.Channel,xInfo.GUID);
                }
            }
        }

        public void StartHeartbeat()
        {
            if(heartbeat_timer == null)
            {
                heartbeat_timer = new Timer(HeartBeat, null, Time_Interval, Timeout.Infinite);
            }
            else 
            {
                heartbeat_timer.Change(Time_Interval, Timeout.Infinite);
            }

            sMsg = Time_Interval / 1000 + "秒后发送心跳信息。";
            Send_Message_Out(sMsg);
        }

        public void HeartBeat(object obj)
        {
            if(socket.status == DB31_Socket.Status.Connected)
            {
                GetStoredDiskSpace();

                string xml_content = xml.HeartbeatXml(DVR_State, Total_Space, Free_Space, Process_Name);
                socket.Send(xml_content);
            }

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
            if(socket.status != DB31_Socket.Status.Connected)
            {
                sMsg = "服务器未联接，请稍后再试。";
                Send_Message_Out(sMsg);

                socket.ReConnect();

                return;
            }

            WorkingStatus = Working_Status.Working;
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

            WorkingStatus = Working_Status.Available;
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
                    WorkingStatus = Working_Status.Available;
                    return;
                }

                if (socket.status != DB31_Socket.Status.Connected)
                {
                    messageContent = "发送失败，服务器未联接。";
                    Send_Message_Out(messageHead + messageContent);

                    socket.ReConnect();

                    return;
                }

                //start form the information
                int Type = (int)OpType;
                int Channel = cam.ChannelNumber;
                DateTime TriggerTime = DateTime.Now;
                string Note = "图像上传";
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

                socket.Send(xml_content);
            }
            messageContent = "图像队列发送完毕。";
            Send_Message_Out(messageHead+messageContent);
        }

        private void GetStoredDiskSpace()
        {
            DeviceSummary.GetStoredDiskSpace("C://", out Total_Space, out Free_Space);
        }
    }
}
