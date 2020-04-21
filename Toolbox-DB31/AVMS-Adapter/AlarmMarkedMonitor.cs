using System;
using System.Collections.Generic;
using System.Text;

using Seer.BaseLibCS;
using Seer.FarmLib.Client;
using Seer.SDK;
using Seer.SDK.NotificationMonitors;

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Data;
using System.IO;
using System.Xml;


namespace Toolbox_DB31.AVMS_Adapter
{


    public class AlarmMarkedMonitor : AbstractSimpleMulticastNotificationMonitor
    {
        public event EventHandler<AlarmMarkedEventArgs> AlarmReceived = delegate { };

        public AlarmMarkedMonitor(INotificationSource source) : base(source)
        {
        }

        protected override Utils.MulticastMessage HandledMessageType
        {
            get { return Utils.MulticastMessage.AlarmMarked; }
        }

        protected override void HandleMulticastNotificationReceived(object sender, MulticastNotificationEventArgs e)
        {
            if (Utils.MulticastMessage.AlarmMarked == e.Notification.MessageType)
            {
                HandleCameraMessage(e);
            }
        }

        private void HandleCameraMessage(MulticastNotificationEventArgs e)
        {
            CServer sourceServer = e.SourceServer;
            byte[] rawData = e.Notification.Data;

            try
            {
                AlarmMarkedStruct msg = Utils.RawDeserializeEx<AlarmMarkedStruct>(rawData);
                if (0 == msg.m_eTmAck)
                {
                    return;
                }
                uint camId = msg.m_aCameraId;
                uint tmAck = msg.m_eTmAck;
                AlarmReceived(this, new AlarmMarkedEventArgs(camId, tmAck));
            }
            catch
            {
            }
        }
    }

    public class AlarmMarkedEventArgs : EventArgs
    {
        public uint CameraId { get; set; }
        public uint TmAlarmMarked { get; set; }

        public AlarmMarkedEventArgs(uint camId, uint tmAck)
        {
            CameraId = camId;
            TmAlarmMarked = tmAck;
        }
    }
}
