using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Toolbox_DB31.Classes;

namespace Toolbox_DB31.DB31_Adapter
{
    class DB31_Controller
    {
        enum OperationCmd_Type
        {
            Test_Image_Upload = 4,
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
        public int Total_Space = 0;
        public int Free_Space = 0;
        public string Process_Name = "System,AI_Main.exe";

        public DB31_Controller(DB31_User db31_user)
        {
            user = db31_user;

            socket = new DB31_Socket();
            socket.Working_Message += OnEvent_Receive_Socket_Message;
            xml = new DB31_Xml();
        }
        private void OnEvent_Receive_Socket_Message(object sender, string sMsg)
        {
            Send_Message_Out(sMsg);
        }
        public void StartHeartbeat()
        {
            string xml_content = xml.HeartbeatXml(DVR_State,Total_Space,Free_Space,Process_Name);
            socket.Send(xml_content);
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

            foreach(Camera_Model cam in cameras)
            {
                if(true == Stop_Uploading_Image)
                {
                    sMsg = "停止上传图像";
                    Send_Message_Out(sMsg);

                    Stop_Uploading_Image = false;
                    WorkingStatus = Working_Status.Available;
                    return;
                }

                //start form the information
                int Type = (int) OpeType;
                int Channel = cam.ChannelNumber;
                DateTime TriggerTime = DateTime.Now;
                string Note = "图像上传";
                string GUID = Guid.NewGuid().ToString();

                string base64image = Global.g_VMS_Adapter.GetEncodedSnapshot(cam.CameraID,TriggerTime,true);
                base64image = "xxxxxxxxxxxxxxxxxxxx";
                string xml_content = xml.OperationCmd_Xml(Type,Channel,TriggerTime.ToString(),Note,GUID,base64image);
                socket.Send(xml_content);

                //Message to the main frame
                if(null != Working_Message )
                {
                    if(OpeType == OperationCmd_Type.Inspection_Image_Upload)
                    {
                        sMsg = "正在上传验收图像：";
                    }
                    sMsg += cam.Name;

                    Send_Message_Out(sMsg);
                    //Thread.Sleep(1000);
                }
            }
            sMsg = "图像上传完毕";
            Send_Message_Out(sMsg);
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
    }
}
