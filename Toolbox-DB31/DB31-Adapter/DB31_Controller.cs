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
            Inspection_Image_Upload = 33
        };

        enum Working_Status { Available, Working };

        private bool Stop_Uploading_Image = false;

        public event Action<object, string> Working_Message;

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
            xml = new DB31_Xml();
        }

        public void StartHeartbeat()
        {
            string xml_content = xml.HeartbeatXml(DVR_State,Total_Space,Free_Space,Process_Name);
            socket.Send(xml_content);
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

            foreach(Camera_Model cam in cameras)
            {
                if(true == Stop_Uploading_Image)
                {
                    Stop_Uploading_Image = false;
                    return;
                }

                string base64image = cam.Get_Base64_Image();
                string xml_content = xml.OperationCmd_Xml();

                if(null != Working_Message )
                {
                    string sMsg="";
                    if(OpeType == OperationCmd_Type.Inspection_Image_Upload)
                    {
                        sMsg = "正在上传验收图像：";
                    }
                    sMsg += cam.Name;

                    Working_Message(this, sMsg);
                    Thread.Sleep(1000);
                }
            }

        }
        public string Inspect_Image_Upload()
        {
            if (true == user.Privilege_Check(DB31_User.Enum_Action.Inspect_Image_Upload))
            {
                new Task(x=>
                { Upload_Image((OperationCmd_Type)x); },OperationCmd_Type.Inspection_Image_Upload).Start();

                //Stop_Uploading_Image = true;

                return "";
            }
 
            return "没有操作权限！";
        }
    }
}
