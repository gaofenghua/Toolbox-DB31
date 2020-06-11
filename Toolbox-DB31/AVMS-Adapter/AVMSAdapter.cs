using System;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using ICSharpCode.SharpZipLib.GZip;
//using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Reflection;
using Seer.DeviceModel.Client;
using Seer.DeviceModel;
using Seer.SDK;
using Seer.SDK.NotificationMonitors;
using Seer.BaseLibCS;
using Seer.FarmLib;
using Seer.FarmLib.Client;
using SeerInterfaces;
using System.Timers;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Specialized;
using System.Configuration;
using System.Xml;


namespace Toolbox_DB31.AVMS_Adapter
{
    public class AVMSAdapter
    {
        private AVMSCom m_avms = null;
        private bool m_bConnectedToAVMSServer = false;
        private bool m_bFarmStateEventHandlerAdded = false;
        private bool m_bDeviceModelEventHandlerAdded = false;
        private bool m_bAVMSListenerEventHandlerAdded = false;
        public bool IsAVMSListeningEnabled { get { return m_bAVMSListenerEventHandlerAdded; } }
        public event AVMSTriggeredHandler AVMSTriggered;
        public delegate void AVMSTriggeredHandler(object sender, AVMSEventArgs e);
        private bool m_bAVMSMessageSend = false;
        private Dictionary<uint, string> m_serverList = new Dictionary<uint, string>();
        private Dictionary<uint, CCamera> m_cameraList = new Dictionary<uint, CCamera>();
        private Dictionary<uint, bool> m_stateList = new Dictionary<uint, bool>();
        private AlarmMonitor m_alarmMonitor = null;
        private AlarmMarkedMonitor m_alarmMarkedMonitor = null;
        private Timer m_timer = null;
        private const int IMPORT_INTERVAL = 65 * 1000;
        private bool m_bPrintLogEnabled = true;

        private const string VIDEO_LOSS_POLICY = "VideoLossPolicy";
        private const string VIDEO_MOTION_DETECT_POLICY = "VMDPolicy";
        private const string HARDWARE_TRIGGER_POLICY = "HWTriggerPolicy";

        private SdkFarm m_farm
        {
            get
            {
                if (null != m_avms)
                {
                    return m_avms.Farm;
                }
                return null;
            }
        }

        public string[] m_servers
        {
            get
            {
                if (null != m_avms)
                {
                    return m_avms.ServerList;
                }
                return null;
            }
        }

        public CDeviceManager m_deviceManager
        {
            get
            {
                if (null != m_farm)
                {
                    return m_farm.DeviceManager;
                }
                return null;
            }
        }

        public void ShowCameraList()
        {
            UpdateCameraList(m_cameraList);
        }

        public void HideCameraList()
        {
            UpdateCameraList(new Dictionary<uint, CCamera>());
        }

        private void UpdateCameraList(Dictionary<uint, CCamera> list)
        {
            App.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                DeviceSummary.UpdateTable(list);
            });
        }

        public void Start(string Ip, string Username, string Password)
        {
            try
            {
                m_avms = new AVMSCom(Ip, Username, Password);
                if (null != m_avms)
                {
                    if (!m_bAVMSMessageSend)
                    {
                        m_avms.MessageSend += new AVMSCom.MessageEventHandler(this.AVMSCom_MessageSend);
                        m_bAVMSMessageSend = true;
                    }
                    m_avms.Connect();
                }

                m_timer = new Timer(IMPORT_INTERVAL);
                m_timer.Enabled = true;
                m_timer.Elapsed += HeartBeat;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Stop()
        {
            try
            {
                if (null != m_avms)
                {
                    DeleteFarmStateEventHandler(m_farm, ref m_bFarmStateEventHandlerAdded);
                    DeleteDeviceModelEventHandler(m_deviceManager, ref m_bDeviceModelEventHandlerAdded);
                    StopAVMSListener();
                    if (m_bAVMSMessageSend)
                    {
                        m_avms.MessageSend -= new AVMSCom.MessageEventHandler(this.AVMSCom_MessageSend);
                        m_bAVMSMessageSend = false;
                    }
                    m_avms.Disconnect();
                    m_avms = null;
                }
                if (null != m_timer)
                {
                    m_timer.Enabled = false;
                    m_timer.Elapsed -= HeartBeat;
                    m_timer = null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void HeartBeat(Object obj, ElapsedEventArgs args)
        {
            DateTime checkDT = args.SignalTime;
            var list = Global.g_CameraList;
            foreach (KeyValuePair<uint, CCamera> item in m_cameraList)
            {
                CCamera cam = item.Value;
                bool isOnline;
                if (m_stateList[cam.CameraId] != (isOnline = IsOnline(cam)))
                {
                    AVMS_ALARM type = isOnline ? AVMS_ALARM.AVMS_ALARM_DEVICERESTORE : AVMS_ALARM.AVMS_ALARM_DEVICELOST;
                    this.OnAVMSTriggered(this, new AVMSEventArgs(type, checkDT, cam.CameraId, null));
                    m_stateList[cam.CameraId] = isOnline;
                }
            }
        }

        public bool RefreshServerManager()
        {
            try
            {
                PopulateServerList();
                return true;
            }
            catch (Exception ex)
            {
                PrintLog("Failed to refresh Server Manager : " + ex.ToString());
                return false;
            }
        }

        public bool RefreshDeviceManager()
        {
            try
            {
                if (null == m_deviceManager)
                {
                    PrintLog("Failed to access Device Manager ： Value null");
                    return false;
                }

                AddDeviceModelEventHandler(m_deviceManager, ref m_bDeviceModelEventHandlerAdded);
                m_deviceManager.Refresh();
                return true;
            }
            catch (Exception ex)
            {
                PrintLog("Failed to refresh Device Manager : " + ex.ToString());
                return false;
            }
        }

        private void Farm_StateChangedEvent(object sender, ValueChangedEventArgs<CFarm.FarmState> e)
        {
            try
            {
                if ((CFarm.FarmState.Connected == e.PreviousValue) && (CFarm.FarmState.Connected != e.NewValue))
                {
                    this.OnAVMSTriggered(this, new AVMSEventArgs(AVMS_ALARM.AVMS_ALARM_CONNECTIONLOST, DateTime.Now, 0, null));
                }

                if ((CFarm.FarmState.Connected != e.PreviousValue) && (CFarm.FarmState.Connected == e.NewValue))
                {
                    this.OnAVMSTriggered(this, new AVMSEventArgs(AVMS_ALARM.AVMS_ALARM_CONNECTED, DateTime.Now, 0, null));
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void DeviceManager_DataLoadedEvent(object sender, EventArgs e)
        {
            try
            {
                PopulateCameraList();
                ShowCameraList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void PopulateServerList()
        {
            if (null == m_avms)
            {
                PrintLog("Fail to connect to AVMS!");
                return;
            }

            m_serverList.Clear();
            string[] servers = m_avms.ServerList;
            uint id = 0;
            foreach (string server in servers)
            {
                m_serverList.Add(id, server);
                PrintLog(String.Format("m_serverList[serverId={0}, server={1}]", id, m_serverList[id]));
                id++;
            }
        }

        private void PopulateCameraList()
        {
            if (null == m_deviceManager)
            {
                PrintLog(String.Format("Fail to load device manager!"));
                return;
            }

            m_cameraList.Clear();
            m_stateList.Clear();
            List<CCamera> cameras = m_deviceManager.GetAllCameras();
            foreach (CCamera cam in cameras)
            {
                uint camId = cam.CameraId;
                m_cameraList.Add(camId, cam);
                m_stateList.Add(camId, IsOnline(cam));
                PrintLog(String.Format("m_cameraList[cameraId={0}, camera={1}]", camId, m_cameraList[camId]));
            }
        }

        private void AddFarmStateEventHandler(SdkFarm farm, ref bool bHandleAdded)
        {
            if ((null != farm) && (!bHandleAdded))
            {
                farm.StateChanged += new EventHandler<ValueChangedEventArgs<CFarm.FarmState>>(Farm_StateChangedEvent);
                bHandleAdded = true;
            }
        }
        private void DeleteFarmStateEventHandler(SdkFarm farm, ref bool bHandleAdded)
        {
            if ((null != farm) && bHandleAdded)
            {
                farm.StateChanged -= new EventHandler<ValueChangedEventArgs<CFarm.FarmState>>(Farm_StateChangedEvent);
                bHandleAdded = false;
            }
        }

        private void AddDeviceModelEventHandler(CDeviceManager deviceManager, ref bool bHandleAdded)
        {
            if ((null != deviceManager) && (!bHandleAdded))
            {
                deviceManager.DataLoadedEvent += new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent);
                bHandleAdded = true;
            }
        }
        private void DeleteDeviceModelEventHandler(CDeviceManager deviceManager, ref bool bHandleAdded)
        {
            if ((null != deviceManager) && bHandleAdded)
            {
                deviceManager.DataLoadedEvent -= new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent);
                bHandleAdded = false;
            }
        }

        public void StartAVMSListener()
        {
            if (null == m_alarmMonitor)
            {
                m_alarmMonitor = new AlarmMonitor(m_farm);
            }
            if (null == m_alarmMarkedMonitor)
            {
                m_alarmMarkedMonitor = new AlarmMarkedMonitor(m_farm);
            }
            AddAVMSListenerEventHandler(ref m_bAVMSListenerEventHandlerAdded);
        }

        public void StopAVMSListener()
        {
            DeleteAVMSListenerEventHandler(ref m_bAVMSListenerEventHandlerAdded);
            m_alarmMonitor = null;
            m_alarmMarkedMonitor = null;
        }

        private void AddAVMSListenerEventHandler(ref bool bHandleAdded)
        {
            if ((null != m_alarmMonitor)
                && (!bHandleAdded))
            {
                m_alarmMonitor.AlarmReceived += new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                m_alarmMarkedMonitor.AlarmReceived += new EventHandler<AlarmMarkedEventArgs>(HandleAlarmMarkedReceived);
                bHandleAdded = true;
            }
        }
        private void DeleteAVMSListenerEventHandler(ref bool bHandleAdded)
        {
            if ((null != m_alarmMonitor)
                && (bHandleAdded))
            {
                m_alarmMonitor.AlarmReceived -= new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                m_alarmMarkedMonitor.AlarmReceived -= new EventHandler<AlarmMarkedEventArgs>(HandleAlarmMarkedReceived);
                m_alarmMonitor.Dispose();
                m_alarmMarkedMonitor.Dispose();
                bHandleAdded = false;
            }
        }

        private void HandleAlarmMessageReceived(object sender, AlarmMessageEventArgs e)
        {
            CameraMessageStruct cameraMessageStruct = e.Message;
            uint camId = cameraMessageStruct.m_iCameraId;
            if (!m_cameraList.ContainsKey(camId))
            {
                PrintLog("AVMSAdapter : fail to handle alarm message due to invalid camera.");
                return;
            }
            AVMS_ALARM alarmType = AVMS_ALARM.AVMS_ALARM_UNKNOWN;
            switch (cameraMessageStruct.m_iEvent)
            {
                case 0:
                case 3:
                case 7:
                    alarmType = GetAlarmType(cameraMessageStruct.m_iPolicyId);
                    break;
                case 8:
                    alarmType = AVMS_ALARM.AVMS_ALARM_DEVICELOST;
                    break;
                default:
                    break;
            }
            DateTime clientTime = TimeUtils.DateTimeFromUTC(cameraMessageStruct.m_utcTime);
            DateTime serverTime = m_cameraList[camId].Server.ToLocalTime(clientTime);
            string picData = GetEncodedSnapshot((int)camId, DateTime.Now, false);

            AVMSEventArgs args = new AVMSEventArgs(alarmType, serverTime, camId, picData);
            this.OnAVMSTriggered(this, args);
        }

        private AVMS_ALARM GetAlarmType(int policyId)
        {
            try
            {
                PolicyConfigSection policies = ConfigurationManager.GetSection("policyConfig") as PolicyConfigSection;
                if (policies.PolicyConfig[VIDEO_LOSS_POLICY].PolicyId == policyId)
                {
                    return AVMS_ALARM.AVMS_ALARM_VIDEOLOSS;
                }
                else if (policies.PolicyConfig[VIDEO_MOTION_DETECT_POLICY].PolicyId == policyId)
                {
                    return AVMS_ALARM.AVMS_ALARM_VMD;
                }
                else if (policies.PolicyConfig[HARDWARE_TRIGGER_POLICY].PolicyId == policyId)
                {
                    return AVMS_ALARM.AVMS_ALARM_HARDWARETRIGGER;
                }
                else
                {
                    return AVMS_ALARM.AVMS_ALARM_UNKNOWN;
                }
            }
            catch
            {
                return AVMS_ALARM.AVMS_ALARM_UNKNOWN;
            }
        }

        private void HandleAlarmMarkedReceived(object sender, AlarmMarkedEventArgs e)
        {
            uint camId = e.CameraId;
            DateTime clientTime = TimeUtils.DateTimeFromUTC(e.TmAlarmMarked);
            DateTime serverTime = m_cameraList[camId].Server.ToLocalTime(clientTime);
            AVMS_ALARM alarmType = AVMS_ALARM.AVMS_ALARM_RESTORE;
            AVMSEventArgs args = new AVMSEventArgs(alarmType, serverTime, camId, string.Empty);
            this.OnAVMSTriggered(this, args);
        }

        public EStates GetCameraStateById(int camId)
        {
            if (!m_cameraList.ContainsKey((uint)camId))
            {
                PrintLog("Invalid camera number!");
                return EStates.Unknown;
            }

            CCamera cam = m_cameraList[(uint)camId];
            return cam.State;
        }

        public string GetEncodedSnapshot(int camId, DateTime dt, bool bSave)
        {
            byte[] byteJpg = GetImageStream(camId, dt);
            if (null == byteJpg)
            {
                PrintLog("GetEncodedSnapshot : stream is not available.");
                return null;
            }

            MemoryStream ms = new MemoryStream(byteJpg, false);
            Image image = (Bitmap)Image.FromStream(ms);
            if (bSave)
            {
                if (!Directory.Exists("image"))
                {
                    Directory.CreateDirectory("image");
                }
                //string fileName = string.Format("cam{0}_{1}.jpg", cam.CameraId.ToString(), dt.ToString("yyyyMMddHHmmss"));
                string fileName = string.Format("cam{0}_{1}.jpg", camId.ToString(), dt.ToString("yyyyMMddHHmmss"));
                string filePath = System.Windows.Forms.Application.StartupPath.ToString() + @"\image\" + fileName;
                image.Save(filePath, ImageFormat.Jpeg);
            }
            return Convert.ToBase64String(byteJpg);
        }

        public string GetAlarm(int camId, DateTime dtStartGMT, DateTime dtEndGMT)
        {
            byte[] byteAlarms = GetAlarmsStream(camId, dtStartGMT, dtEndGMT);
            if (null == byteAlarms)
            {
                PrintLog("GetAlarm : signal is not available.");
                return null;
            }

            switch (byteAlarms[0])
            {
                case 1:
                    MemoryStream compStream = new MemoryStream(byteAlarms, 1, byteAlarms.Length - 1, false);
                    GZipInputStream uncompStream = new GZipInputStream(compStream);

                    DataSet ret = new DataSet();
                    ret.ReadXml(uncompStream);

                    string log = string.Empty;
                    if ((null != ret) && (ret.Tables.Count > 0))
                    {
                        DataTable dt = ret.Tables[0];
                        int iRows = dt.Rows.Count;
                        for (int i = 0; i < iRows; i++)
                        {
                            DataRow dr = dt.Rows[i];
                            log += string.Join(",", dr.ItemArray) + ((i == iRows - 1) ? string.Empty : "\r\n");
                        }
                    }
                    return log;

                default:
                    PrintLog("GetAlarmLog : could not retreive alarm list from server.");
                    return null;
            }
        }

        public string GetEvent(int camId, DateTime dtStartGMT, DateTime dtEndGMT)
        {
            if (null == m_avms)
            {
                PrintLog("Fail to connect to AVMS!");
                return null;
            }

            if (!m_farm.CanAccess(FarmRight.ViewAlarm) || dtStartGMT >= dtEndGMT)
            {
                PrintLog("Fail to get alarms!");
                return null;
            }

            if (!m_cameraList.ContainsKey((uint)camId))
            {
                PrintLog("Invalid camera number!");
                return null;
            }

            CCamera cam = m_cameraList[(uint)camId];
            DataSet dsEvents = null;
            bool isExported = m_avms.ExportEventsStream(cam, dtStartGMT, dtEndGMT, ref dsEvents);
            if (!isExported)
            {
                return null;
            }

            string log = string.Empty;
            DataTable dt = dsEvents.Tables[0];
            int iRows = dt.Rows.Count;
            for (int i = 0; i < iRows; i++)
            {
                DataRow dr = dt.Rows[i];
                log += string.Join(",", dr.ItemArray) + ((i == iRows - 1) ? string.Empty : "\r\n");
            }
            return log;
        }

        public bool InsertAlarm(CCamera cam, int alarmTime, int policyID, string alarmText1, string alarmText2)
        {
            return m_avms.AddAlarm(cam, alarmTime, policyID, alarmText1, alarmText2);
        }

        public byte[] GetAlarmsStream(int camId, DateTime dtStartGMT, DateTime dtEndGMT)
        {
            if (null == m_avms)
            {
                PrintLog("Fail to connect to AVMS!");
                return null;
            }

            if (!m_farm.CanAccess(FarmRight.ViewAlarm) || dtStartGMT >= dtEndGMT)
            {
                PrintLog("Fail to get alarms!");
                return null;
            }

            if (!m_cameraList.ContainsKey((uint)camId))
            {
                PrintLog("Invalid camera number!");
                return null;
            }

            CCamera cam = m_cameraList[(uint)camId];
            byte[] byteSignals = null;
            bool isExported = m_avms.ExportAlarmsStream(cam, dtStartGMT, dtEndGMT, ref byteSignals);
            if (!isExported)
            {
                return null;
            }
            return byteSignals;
        }

        public byte[] GetImageStream(int camId, DateTime dt)
        {
            if (null == m_avms)
            {
                PrintLog("Fail to connect to AVMS!");
                return null;
            }

            if (!m_cameraList.ContainsKey((uint)camId))
            {
                PrintLog("Invalid camera number!");
                return null;
            }

            CCamera cam = m_cameraList[(uint)camId];
            DateTime jpgTime = cam.Server.ToUtcTime(dt);
            bool bViewPrivateVideo = cam.CanAccess(DeviceRight.ViewPrivateVideo);
            string sFilename = string.Empty;
            byte[] byteJpg = null;
            bool isExported = m_avms.ExportImageStream(cam, jpgTime, bViewPrivateVideo, ref sFilename, ref byteJpg);
            if (!isExported)
            {
                return null;
            }
            return byteJpg;
        }

        public string GetStoredPath()
        {
            if ((null == m_avms) || 0 == m_cameraList.Count)
            {
                PrintLog("Fail to retrieve AVMS data store information!");
                return null;
            }

            return m_avms.GetDataPath(m_cameraList.Values.First());
        }

        private void OnAVMSTriggered(object sender, AVMSEventArgs e)
        {
            if (null != AVMSTriggered)
            {
                this.AVMSTriggered(sender, e);
            }
        }

        private void AVMSCom_MessageSend(object sender, MessageEventArgs e)
        {
            string methodName = MethodBase.GetCurrentMethod().Name;

            string message = e.Message;
            if ((string.Empty == message) || (2 != message.Split('\t').Length))
            {
                PrintLog("AVMSCom_MessageSend : invalid message.");
                return;
            }

            string time = message.Split('\t')[0];
            string state = string.Empty;
            switch (message.Split('\t')[1])
            {
                case "Connect":

                    m_bConnectedToAVMSServer = m_avms.IsConnected;
                    if (m_bConnectedToAVMSServer)
                    {
                        PrintLog(string.Format("{0} - AVMSCom_MessageSend : AVMS connection has been established.", time));
                        this.OnAVMSTriggered(this, new AVMSEventArgs(AVMS_ALARM.AVMS_ALARM_CONNECTED, DateTime.Now, 0, null));
                        AddFarmStateEventHandler(m_farm, ref m_bFarmStateEventHandlerAdded);
                        RefreshServerManager();
                        RefreshDeviceManager();
                    }

                    break;

                case "Disconnect":

                    m_bConnectedToAVMSServer = m_avms.IsConnected;
                    if (!m_bConnectedToAVMSServer)
                    {
                        PrintLog(string.Format("{0} - AVMSCom_MessageSend : AVMS connection has been broken.", time));
                    }

                    break;

                default:
                    PrintLog(string.Format("{0} - AVMSCom_MessageSend : [{1}]", time, message.Split('\t')[1]));
                    break;
            }

            Global.g_VMS_Adapter = this;
        }

        private void PrintLog(string text)
        {
            if (m_bPrintLogEnabled)
            {
                Trace.WriteLine(text);
            }
        }

        public static bool IsOnline(CCamera cam)
        {
            return cam.State == EStates.Off ? false : true;
        }
    }

    public enum AVMS_ALARM
    {
        AVMS_ALARM_UNKNOWN = 0,
        AVMS_ALARM_CONNECTED,       // AVMS连接建立
        AVMS_ALARM_CONNECTIONLOST,  // AVMS连接丢失
        AVMS_ALARM_DEVICELOST,      // 设备丢失
        AVMS_ALARM_DEVICERESTORE,      // 设备连接恢复
        AVMS_ALARM_RESTORE,         // 报警信息恢复
        AVMS_ALARM_VIDEOLOSS,       // 视频丢失报警
        AVMS_ALARM_VMD,             // 移动侦测报警
        AVMS_ALARM_HARDWARETRIGGER, // 硬件触发报警
        AVMS_ALARM_OTHER,
    }

    public class AVMSEventArgs : EventArgs
    {
        public AVMS_ALARM m_alarmType { get; set; }
        public DateTime m_alarmTime { get; set; }
        public uint m_cameraId { get; set; }
        public string m_pictureData { get; set; }

        public AVMSEventArgs(AVMS_ALARM type, DateTime time, uint id, string data)
        {
            this.m_alarmType = type;
            this.m_alarmTime = time;
            this.m_cameraId = id;
            this.m_pictureData = data;
        }
    }

    public class PolicyConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("", IsDefaultCollection = true)]
        public PolicyInfoCollection PolicyConfig
        {
            get { return (PolicyInfoCollection)base[""]; }
        }
    }

    public class PolicyInfoCollection : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new PolicyInfo();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((PolicyInfo)element).PolicyName;
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "policy"; }
        }

        public PolicyInfo this[int index]
        {
            get { return BaseGet(index) as PolicyInfo; }
        }

        public new PolicyInfo this[string name]
        {
            get { return BaseGet(name) as PolicyInfo; }
        }
    }

    public class PolicyInfo : ConfigurationElement
    {
        [ConfigurationProperty("key", IsRequired = true)]
        public string PolicyName
        {
            get { return (string)this["key"]; }
            set { this["key"] = value; }
        }

        [ConfigurationProperty("value", IsRequired = true)]
        public int PolicyId
        {
            get { return (int)this["value"]; }
            set { this["value"] = value; }
        }
    }
}
