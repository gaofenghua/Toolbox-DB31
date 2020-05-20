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

        private void SetAgentID(string AgentID)
        {
            xml_Agent.SetAttributeValue("ID", AgentID);
        }
        public string HeartbeatXml(string AgentID, int state,long total_space,long free_space,string process_name)
        {
           
            xml_DVRHeart.SetAttributeValue("state", state);
            xml_DVRHeart.SetAttributeValue("TotalSpace", total_space);
            xml_DVRHeart.SetAttributeValue("FreeSpace", free_space);
            xml_DVRHeart.Value = process_name;

            xml_Agent.ReplaceNodes(xml_DVRHeart);

            xml_Agent.Add(xml_GetTicks);

            SetAgentID(AgentID);
            
            return xml_Declaration.ToString() + xml_Agent.ToString();
        }

        public string OperationCmd_Xml(string AgentID, int Type, int Channel, string TriggerTime, string Note, string GUID, string Base64Image)
        {
            SetAgentID(AgentID);

            xml_OperationCmd.SetAttributeValue("Type", Type);
            xml_OperationCmd.SetAttributeValue("Channel", Channel);
            xml_OperationCmd.SetAttributeValue("TriggerTime", TriggerTime);
            xml_OperationCmd.SetAttributeValue("Note", Note);
            xml_OperationCmd.SetAttributeValue("GUID", GUID);
            xml_OperationCmd.Value = Base64Image==null?"":Base64Image;

            xml_Agent.ReplaceNodes(xml_OperationCmd);
            return xml_Declaration.ToString() + xml_Agent.ToString();
        }

        public Xml_Parse_Output ParseXml(string sXml)
        {
            Xml_Parse_Output xRet = new Xml_Parse_Output();


            XDocument xd = XDocument.Parse(sXml);
            int nQuery = 0;
            XElement item;

            //Get the OK server time
            var query = from s in xd.Descendants()
                        where s.Name.LocalName == "OK" && s.Parent.Name.LocalName == "Server"
                        select s;

            nQuery = query.Count();

            if(nQuery == 1)
            {
                item = query.First();
                if (null != item.Attribute("NowTime"))
                {
                    xRet.OK_NowTime = item.Attribute("NowTime").Value;
                }
            }

            //Get the ticks 
            query = from s in xd.Descendants()
                    where s.Name.LocalName == "Ticks" && s.Parent.Name.LocalName == "Server"
                    select s;

            nQuery = query.Count();

            if (nQuery == 1)
            {
                item = query.First();
                if (null != item.Attribute("Value"))
                {
                    string sTicks = item.Attribute("Value").Value;
                    int.TryParse(sTicks, out xRet.Ticks);
                }

                //若收到的心跳包含GetImage字段，DVR将立刻用OperationCmd发送指定各个通道的实时图片；OperationCmd中GUID属性填写服务器回传的GUID编号；Type属性为5(Type=5);需上传通道为“，”隔开的通道编号。
                //
                //Get GetImage 
                query = from s in xd.Descendants()
                        where s.Name.LocalName == "GetImage" && s.Parent.Name.LocalName == "Server"
                        select s;

                nQuery = query.Count();

                if(nQuery ==1)
                {
                    item = query.First();
                    if (null != item.Attribute("Channel"))
                    {
                        xRet.Channel = item.Attribute("Channel").Value;
                    }
                    if (null != item.Attribute("GUID"))
                    {
                        xRet.GUID = item.Attribute("GUID").Value;
                    }
                }
            }

            return xRet;
        }
    }

    public class Xml_Parse_Output
    {
        public string OK_NowTime = null;
        public int Ticks = 0;
        public string Channel = null;
        public string GUID = null;
    }
}
