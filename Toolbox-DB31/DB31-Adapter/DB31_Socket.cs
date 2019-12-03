using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using socket.framework.Client;

namespace Toolbox_DB31.DB31_Adapter
{
    class DB31_Socket
    {
        public TcpPushClient client;

        public DB31_Socket()
        {
            string ip_add="192.168.77.201";
            int port_num=5901;

            client = new TcpPushClient(1024);
            client.OnConnect += Client_OnConnect;
            client.OnReceive += Client_OnReceive;
            client.OnSend += Client_OnSend;
            client.OnClose += Client_OnClose;
            client.OnDisconnect += Client_OnDisconnect;

            client.Connect(ip_add, port_num);
        }
        private void Client_OnConnect(bool obj)
        {
            bool ret = obj;

            //通讯指令
            //分2个部分，消息头 + 消息内容
            //消息头:
            //固定为20字节
            //6字节：QWCMD:
            //4字节：消息内容长度(包含消息头长度)
            //10字节：保留
            //消息内容为XML格式文本。

            byte[] data_head = new byte[20] { (byte)'Q', (byte)'W', (byte)'C', (byte)'M', (byte)'D', (byte)':',0,0,0,0,0,0,0,0,0,0,0,0,0,0 };
            
            string xmlData = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><Agent ID=\"SSJCZHQY0001\" Type=\"SG\" Ver=\"1.3.0.0\"><DVRHeart state=\"1\">System</DVRHeart><GetTicks/><OperationCmd Type=\"1\" Channel=\"0\" TriggerTime=\"2019-12-01 02: 03: 05\" Note=\"XXXXXX\" GUID=\"123456789\">4AAQSkZJRgABAQEASAB2wBDABsSFBcUERsXFhceHBsgKEIrKCUlKFE6PTBCYFVlZF9V</OperationCmd></Agent>";
            byte[] data_xml = Encoding.UTF8.GetBytes(xmlData);

            int dataLength = data_xml.Length + 20;
            byte[] data_Length = BitConverter.GetBytes(dataLength);
            Buffer.BlockCopy(data_Length, 0, data_head, 6, 4);

            byte[] data_send = new byte[dataLength];
            Buffer.BlockCopy(data_head, 0, data_send, 0, data_head.Length);
            Buffer.BlockCopy(data_xml, 0, data_send, data_head.Length, data_xml.Length);

            client.Send(data_send, 0, data_send.Length);

            string debug_string = Encoding.UTF8.GetString(data_send);
        }
        public void Client_OnReceive(byte[] obj)
        { }
        public void Client_OnSend(int obj)
        { }
        private void Client_OnClose()
        {
            
        }
        private void Client_OnDisconnect()
        {
           
        }

        
       

    }
}
