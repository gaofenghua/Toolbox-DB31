using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Toolbox_DB31.DB31_Adapter
{
    class DB31_Xml
    {
        XDeclaration xml_Declaration;
        XElement xml_Agent;
        XElement xml_DVRHeart;
        XElement xml_GetTicks = new XElement("GetTicks");
        XElement xml_OperationCmd;

        //string xmlData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Agent ID=\"SSJCZHQY0001\" Type=\"SG\" Ver=\"1.3.0.0\"><DVRHeart state=\"1\">System</DVRHeart><GetTicks/><OperationCmd Type=\"1\" Channel=\"0\" TriggerTime=\"2019-12-01 02: 03: 05\" Note=\"XXXXXX\" GUID=\"123456789\">4AAQSkZJRgABAQEASAB2wBDABsSFBcUERsXFhceHBsgKEIrKCUlKFE6PTBCYFVlZF9V</OperationCmd></Agent>";
        public DB31_Xml()
        {
            Declaration();
            DVRHeart();
            OperationCmd();
            Agent();
        }
        private string Declaration()
        {
            xml_Declaration = new XDeclaration("1.0", "UTF-8",null);

            return xml_Declaration.ToString();
        }
        private string DVRHeart()
        {
            int DVRHeart_state = 0;

            xml_DVRHeart = new XElement("DVRHeart",
               new XAttribute("state", DVRHeart_state),
               "System");

            string sRet = xml_DVRHeart.ToString();

            return sRet;
        }
        private void OperationCmd()
        {
            xml_OperationCmd = new XElement("OperationCmd");
        }
        private string Agent()
        {
            xml_Agent = new XElement("Agent",
                new XAttribute("ID", "SSJCZHQY0001"),
                new XAttribute("Type", "SG"),
                new XAttribute("Ver", "1.3.0.0")
                );

            return xml_Agent.ToString();
        }

        private string xmldoc_Heartbeat()
        {
            XDocument doc_heartbeat = new XDocument(
                
                xml_Agent 
                );
            doc_heartbeat.Declaration = xml_Declaration;

            xml_Agent.Add(xml_DVRHeart);
           
            return doc_heartbeat.ToString();
        }

        public string HeartbeatXml(int state,int total_space,int free_space,string process_name)
        {
           
            xml_DVRHeart.SetAttributeValue("state", state);
            xml_DVRHeart.SetAttributeValue("TotalSpace", total_space);
            xml_DVRHeart.SetAttributeValue("FreeSpace", free_space);
            xml_DVRHeart.Value = process_name;

            xml_Agent.ReplaceNodes(xml_DVRHeart);

            xml_Agent.Add(xml_GetTicks);
            
            return xml_Declaration.ToString() + xml_Agent.ToString();
        }

        public string OperationCmd_Xml(int Type, int Channel, string TriggerTime, string Note, string GUID, string Base64Image)
        {
            xml_OperationCmd.SetAttributeValue("Type", Type);
            xml_OperationCmd.SetAttributeValue("Channel", Channel);
            xml_OperationCmd.SetAttributeValue("TriggerTime", TriggerTime);
            xml_OperationCmd.SetAttributeValue("Note", Note);
            xml_OperationCmd.SetAttributeValue("GUID", GUID);
            xml_OperationCmd.Value = Base64Image==null?"":Base64Image;

            xml_Agent.ReplaceNodes(xml_OperationCmd);
            return xml_Declaration.ToString() + xml_Agent.ToString();
        }
    }
}
