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
        public DB31_Controller controller { get; set; } = null;

        XDeclaration xml_Declaration;
        XElement xml_Agent;
        XElement xml_DVRHeart;

        //string xmlData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Agent ID=\"SSJCZHQY0001\" Type=\"SG\" Ver=\"1.3.0.0\"><DVRHeart state=\"1\">System</DVRHeart><GetTicks/><OperationCmd Type=\"1\" Channel=\"0\" TriggerTime=\"2019-12-01 02: 03: 05\" Note=\"XXXXXX\" GUID=\"123456789\">4AAQSkZJRgABAQEASAB2wBDABsSFBcUERsXFhceHBsgKEIrKCUlKFE6PTBCYFVlZF9V</OperationCmd></Agent>";
        public DB31_Xml()
        {
            Declaration();
            DVRHeart();
            Agent();


        }
        public string Declaration()
        {
            xml_Declaration = new XDeclaration("1.0", "UTF-8",null);

            return xml_Declaration.ToString();
        }
        public string DVRHeart()
        {
            int DVRHeart_state = 0;

            xml_DVRHeart = new XElement("DVRHeart",
               new XAttribute("state", DVRHeart_state),
               "System");

            string sRet = xml_DVRHeart.ToString();

            return sRet;
        }

        public string Agent()
        {
            xml_Agent = new XElement("Agent",
                new XAttribute("ID", "SSJCZHQY0001"),
                new XAttribute("Type", "SG"),
                new XAttribute("Ver", "1.3.0.0")
                );

            return xml_Agent.ToString();
        }

        public string xmldoc_Heartbeat()
        {
            XDocument doc_heartbeat = new XDocument(
                
                xml_Agent 
                );
            doc_heartbeat.Declaration = xml_Declaration;

            xml_Agent.Add(xml_DVRHeart);
           
            return doc_heartbeat.ToString();
        }

        public string Heart_Signal()
        {
            xml_DVRHeart.SetAttributeValue("state", 5);
            xml_DVRHeart.SetAttributeValue("TotalSpace", "6");
            xml_DVRHeart.SetAttributeValue("FreeSpace", "3");

            xml_Agent.ReplaceNodes(xml_DVRHeart);
            //xml_Agent.Add(xml_DVRHeart);

            return xml_Declaration.ToString() + xml_Agent.ToString();
        }

        public string HeartbeatXml()
        {
            if(controller == null)
            {
                return null;
            }

            xml_DVRHeart.SetAttributeValue("state", controller.DVR_State);
            xml_DVRHeart.SetAttributeValue("TotalSpace", controller.Total_Space);
            xml_DVRHeart.SetAttributeValue("FreeSpace", controller.Free_Space);

            xml_Agent.ReplaceNodes(xml_DVRHeart);
            
            return xml_Declaration.ToString() + xml_Agent.ToString();
        }
    }
}
