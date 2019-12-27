﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
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
using Toolbox_DB31.Classes;


namespace Toolbox_DB31.AVMS_Adapter
{
    public class AVMSAdapter
    {
        private string m_agentId = string.Empty;
        private AVMSCom m_avms = null;
        private bool m_bConnectedToAVMSServer = false;
        private bool m_bDeviceModelEventHandlerAdded = false;
        private bool m_bAVMSListenerEventHandlerAdded = false;
        public event AVMSTriggeredHandler AVMSTriggered;
        public delegate void AVMSTriggeredHandler(object sender, AVMSEventArgs e);
        private bool m_bAVMSMessageSend = false;
        private Dictionary<uint, string> m_serverList = new Dictionary<uint, string>();
        private Dictionary<uint, CCamera> m_cameraList = new Dictionary<uint, CCamera>();
        private AlarmMonitor m_alarmMonitor = null;

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

        private void UpdateCameraList()
        {
            App.Current.Dispatcher.BeginInvoke((Action)delegate ()
            {
                foreach (KeyValuePair<uint, CCamera> item in m_cameraList)
                {
                    Global.g_CameraList.Add(new Camera_Model() { AgentID = m_agentId, ChannelNumber = (int)item.Key, Name = item.Value.Name, IsSelected = false });
                }
            });
        }

        public void Start(string Ip, string Username, string Password, string Id)
        {
            try
            {
                m_agentId = Id;
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
            }
            catch (Exception ex)
            {
                throw ex;
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

        private void DeviceManager_DataLoadedEvent(object sender, EventArgs e)
        {
            try
            {
                PopulateCameraList();
                UpdateCameraList();
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
            List<CCamera> cameras = m_deviceManager.GetAllCameras();
            foreach (CCamera cam in cameras)
            {
                uint camId = cam.CameraId;
                m_cameraList.Add(camId, cam);
                PrintLog(String.Format("m_cameraList[cameraId={0}, camera={1}]", camId, m_cameraList[camId]));
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
            AddAVMSListenerEventHandler(ref m_bAVMSListenerEventHandlerAdded);
        }

        public void StopAVMSListener()
        {
            DeleteAVMSListenerEventHandler(ref m_bAVMSListenerEventHandlerAdded);
            m_alarmMonitor = null;
        }

        private void AddAVMSListenerEventHandler(ref bool bHandleAdded)
        {
            if ((null != m_alarmMonitor)
                && (!bHandleAdded))
            {
                m_alarmMonitor.AlarmReceived += new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                bHandleAdded = true;
            }
        }
        private void DeleteAVMSListenerEventHandler(ref bool bHandleAdded)
        {
            if ((null != m_alarmMonitor)
                && (bHandleAdded))
            {
                m_alarmMonitor.AlarmReceived -= new EventHandler<AlarmMessageEventArgs>(HandleAlarmMessageReceived);
                bHandleAdded = false;
            }
        }

        private void HandleAlarmMessageReceived(object sender, AlarmMessageEventArgs e)
        {
            CameraMessageStruct cameraMessageStruct = e.Message;

            CameraLogStruct cameraLogStruct = new CameraLogStruct();
            cameraLogStruct.m_iAlarmDbId = cameraMessageStruct.m_iAlarmDbId;
            cameraLogStruct.m_iCameraId = cameraMessageStruct.m_iCameraId;
            cameraLogStruct.m_iEvent = cameraMessageStruct.m_iEvent;
            cameraLogStruct.m_iFarmId = cameraMessageStruct.m_iFarmId;
            cameraLogStruct.m_iFGCount = cameraMessageStruct.m_iFGCount;
            cameraLogStruct.m_iNotUsed2 = cameraMessageStruct.m_iNotUsed2;
            cameraLogStruct.m_iPolicyId = cameraMessageStruct.m_iPolicyId;
            cameraLogStruct.m_iState = cameraMessageStruct.m_iState;
            cameraLogStruct.m_iVersion = cameraMessageStruct.m_iVersion;
            cameraLogStruct.m_milliSinceChangeBegan = cameraMessageStruct.m_milliSinceChangeBegan;
            cameraLogStruct.m_milliTime = cameraMessageStruct.m_milliTime;
            cameraLogStruct.m_timezoneTime = cameraMessageStruct.m_timezoneTime;
            cameraLogStruct.m_utcTime = cameraMessageStruct.m_utcTime;
            cameraLogStruct.m_strConfirm = "Not Confirmed";
            cameraLogStruct.m_strComment = string.Empty;

            // snapshot (Base64)
            if (!m_cameraList.ContainsKey(cameraLogStruct.m_iCameraId))
            {
                PrintLog("AVMSAdapter : fail to handle alarm message due to invalid camera.");
                return;
            }
            CCamera cam = m_cameraList[cameraLogStruct.m_iCameraId];
            string picData = GetEncodedSnapshot(cam, DateTime.Now, true);
            AVMSEventArgs args = new AVMSEventArgs(cameraLogStruct, picData);
            this.OnAVMSTriggered(this, args);
        }

        public string GetEncodedSnapshot(CCamera cam, DateTime dt, bool bSave)
        {
            byte[] byteJpg = GetImageStream(cam, dt);
            if (null == byteJpg)
            {
                PrintLog("TakeSnapshot : not available.");
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
                string fileName = string.Format("cam{0}_{1}.jpg", cam.CameraId.ToString(), dt.ToString("yyyyMMddHHmmss"));
                string filePath = System.Windows.Forms.Application.StartupPath.ToString() + @"\image\" + fileName;
                image.Save(filePath, ImageFormat.Jpeg);
            }
            return Convert.ToBase64String(byteJpg);
        }

        public bool InsertAlarm(CCamera cam, int alarmTime, int policyID, string alarmText1, string alarmText2)
        {
            return m_avms.AddAlarm(cam, alarmTime, policyID, alarmText1, alarmText2);
        }

        public byte[] GetSignalsStream(CCamera cam, DateTime dtStartGMT, DateTime dtEndGMT)
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

            byte[] byteSignals = null;
            bool isExported = m_avms.ExportSignalsStream(cam, dtStartGMT, dtEndGMT, ref byteSignals);
            if (!isExported)
            {
                return null;
            }
            return byteSignals;
        }

        public byte[] GetImageStream(CCamera cam, DateTime dt)
        {
            if (null == m_avms)
            {
                PrintLog("Fail to connect to AVMS!");
                return null;
            }

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
                        RefreshServerManager();
                        RefreshDeviceManager();
                        StartAVMSListener();    //
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
        }

        private void PrintLog(string text)
        {
            Trace.WriteLine(text);
        }
    }

    public struct CameraLogStruct
    {
        public uint m_iAlarmDbId;
        public uint m_iCameraId;
        public uint m_iEvent;
        public uint m_iFarmId;
        public uint m_iFGCount;
        public uint m_iNotUsed2;
        public int m_iPolicyId;
        public uint m_iState;
        public uint m_iVersion;
        public uint m_milliSinceChangeBegan;
        public ushort m_milliTime;
        public short m_timezoneTime;
        public uint m_utcTime;
        //
        public string m_strConfirm;
        public string m_strComment;
    }

    public class AVMSEventArgs : EventArgs
    {
        public object m_eventData { get; }  // CameraLogStruct
        public string m_pictureData { get; }

        public AVMSEventArgs(object obj, string data)
        {
            this.m_eventData = obj;
            this.m_pictureData = data;
        }
    }
}