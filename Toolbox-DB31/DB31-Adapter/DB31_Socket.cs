using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using socket.framework.Client;
using System.Threading;

namespace Toolbox_DB31.DB31_Adapter
{
    class DB31_Socket
    {
        public enum Status { Initial,Connecting, Connected, Disconnected };
        public Status status = Status.Initial;

        public event Action<object, string> Working_Message;
        string sMsg = "";

        public event Action<object, string> Data_Received;

        public TcpPushClient client;
        string ip_add = "192.168.43.63";
        int port_num = 5901;

        //通讯指令
        //分2个部分，消息头 + 消息内容
        //消息头:
        //固定为20字节
        //6字节：QWCMD:
        //4字节：消息内容长度(包含消息头长度)
        //10字节：保留
        //消息内容为XML格式文本。
        byte[] data_head = new byte[20] { (byte)'Q', (byte)'W', (byte)'C', (byte)'M', (byte)'D', (byte)':', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public DB31_Socket()
        {
            client = new TcpPushClient(1024);
            client.OnConnect += Client_OnConnect;
            client.OnReceive += Client_OnReceive;
            client.OnSend += Client_OnSend;
            client.OnClose += Client_OnClose;
            client.OnDisconnect += Client_OnDisconnect;

            client.Connect(ip_add, port_num);
            status = Status.Connecting;
        }
        public void ReConnect()
        {
            client.Connect(ip_add, port_num);
            status = Status.Connecting;

            sMsg = "重新连接服务器: " + ip_add + ":" + port_num;
            Send_Message_Out(sMsg);
        }
        private void Client_OnConnect(bool obj)
        {
            if(false == obj)
            {
                status = Status.Disconnected;

                Thread.Sleep(2000);
                sMsg = ip_add + ":" + port_num + " 服务器连接失败。";
                Send_Message_Out(sMsg);
            }
            else
            {
                status = Status.Connected;

                Thread.Sleep(2000);
                sMsg = ip_add + ":" + port_num + " 服务器已连接。";
                Send_Message_Out(sMsg);
            }
        }
        public void Client_OnReceive(byte[] obj)
        {
            //Check the data head
            if(null == obj || obj.Length <= 20)
            {
                return;
            }
            
            for(int i=0;i<6;i++)
            {
                if(obj[i]!=data_head[i])
                {
                    return;
                }
            }

            int contentLength = BitConverter.ToInt32(obj, 6);
            //Get the xml data
            string xmlData = Encoding.UTF8.GetString(obj,20,obj.Length-20);

            //Trigger data received event
            if(Data_Received!=null)
            {
                Data_Received(this, xmlData);
            }
        }
        public void Client_OnSend(int obj)
        { }
        private void Client_OnClose()
        {
            status = Status.Disconnected;

            Thread.Sleep(2000);
            sMsg = ip_add + ":" + port_num + " 已断开服务器。";
            Send_Message_Out(sMsg);
        }
        private void Client_OnDisconnect()
        {
            status = Status.Disconnected;

            Thread.Sleep(2000);
            sMsg = ip_add + ":" + port_num + " 服务器连接被断开。";
            Send_Message_Out(sMsg);
        }

        public void Send(string xmlData)
        {
            //通讯指令
            //分2个部分，消息头 + 消息内容
            //消息头:
            //固定为20字节
            //6字节：QWCMD:
            //4字节：消息内容长度(包含消息头长度)
            //10字节：保留
            //消息内容为XML格式文本。
            

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

        private void Send_Message_Out(string sMsg)
        {
            if(Working_Message != null)
            {
               
                Working_Message(this, sMsg);
            }
        }

        public void Close()
        {
            client.Close();
            status = Status.Disconnected;
        }
    }
}
