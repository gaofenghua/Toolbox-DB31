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
        public string Process_Name = "System,AI_Main.exe";

        public DB31_Controller()
        {
            socket = new DB31_Socket();
            xml = new DB31_Xml();
        }

        public void StartHeartbeat()
        {
            string xml_content = xml.HeartbeatXml(DVR_State,Total_Space,Free_Space,Process_Name);
            socket.Send(xml_content);
        }
    }
}
