using System;
using DevExpress.Mvvm;
using Toolbox_DB31.AVMS_Adapter;
using System.Linq;

namespace Toolbox_DB31.Classes
{
    public class SettingsMenuViewModel
    {
        public long LogUpdateDuration { get; set; }
        public bool IsDailyTimerEnabled { get; set; }
        public DateTime DailyUpdateDateTime { get; set; }
        public long AlarmUpdateBefore { get; set; }
        public long AlarmUpdateAfter { get; set; }
        public long AlarmUpdateDuration { get; set; }

        public SettingsMenuViewModel()
        {
            LogUpdateDuration = 0;
            IsDailyTimerEnabled = false;
            DailyUpdateDateTime = DateTime.Now;
            AlarmUpdateBefore = 0;
            AlarmUpdateAfter = 0;
            AlarmUpdateDuration = 0;
        }

        public bool IsAlarmListeningEnabled()
        {
            if (null == Global.g_VMS_Adapter)
            {
                return false;
            }
            return Global.g_VMS_Adapter.IsAVMSListeningEnabled;
        }

        public void StartAlarmListening()
        {
            Global.g_DB31_Adapter.Seconds_Before_Alarm = (int)AlarmUpdateBefore;
            Global.g_DB31_Adapter.Seconds_After_Alarm = (int)AlarmUpdateAfter;
            Global.g_DB31_Adapter.Alarm_Interval = (int)AlarmUpdateDuration;

            Global.g_VMS_Adapter.StartAVMSListener();
        }

        public void StopAlarmListening()
        {
            Global.g_VMS_Adapter.StopAVMSListener();
        }

        public string GetEventLog(int camId)
        {
            if (null == Global.g_VMS_Adapter)
            {
                return null;
            }

            DateTime dtStop = DateTime.Now;
            DateTime dtStart = dtStop.AddHours(0 - LogUpdateDuration);
            return Global.g_VMS_Adapter.GetEvent(camId, dtStart, dtStop);
        }

        public string GetAlarmLog(int camId)
        {
            if (null == Global.g_VMS_Adapter)
            {
                return null;
            }

            DateTime dtStop = DateTime.Now;
            DateTime dtStart = dtStop.AddHours(0 - LogUpdateDuration);
            return Global.g_VMS_Adapter.GetAlarm(camId, dtStart, dtStop);
        }
    }
}