﻿using System;
using DevExpress.Mvvm;
using Toolbox_DB31.AVMS_Adapter;

namespace Toolbox_DB31.Classes
{
    public class SettingsMenuViewModel
    {
        public long LogUpdateDuration { get; set; }
        public bool IsDailyTimerEnabled { get; set; }
        public long AlarmUpdateBefore { get; set; }
        public long AlarmUpdateAfter { get; set; }
        public long AlarmUpdateDuration { get; set; }

        public SettingsMenuViewModel()
        {
            LogUpdateDuration = 0;
            IsDailyTimerEnabled = false;
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
            Global.g_VMS_Adapter.StartAVMSListener();
        }

        public void StopAlarmListening()
        {
            Global.g_VMS_Adapter.StopAVMSListener();
        }
    }
}