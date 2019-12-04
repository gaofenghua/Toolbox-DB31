using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox_DB31.DB31_Adapter
{
    class DB31_Controller
    {
        DB31_Socket socket;
        DB31_Xml xml;

        public int DVR_State = 0;
        public int Total_Space = 0;
        public int Free_Space = 0;
        public string Process_Name = "AI_Main.exe";

        public string xml_content { get; set; } = null;
        public DB31_Controller()
        {
            socket = new DB31_Socket();
            socket.controller = this;

            xml = new DB31_Xml();
            xml.controller = this;
        }

        public void StartHeartbeat()
        {
            xml_content = xml.HeartbeatXml();
            socket.Send();
        }
    }
}
