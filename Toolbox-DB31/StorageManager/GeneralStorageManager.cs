using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;

namespace Toolbox_DB31
{
    public class GeneralStorageManager : StorageManager
    {
        private Timer m_timer = null;
        private const int CHECK_INTERVAL = 30 * 1000;

        public GeneralStorageManager()
        {
            m_storageOwner = STORAGE_MANUFACTURER.MANUFACTURER_UNKNOWN;
        }

        public override void Load()
        {
            m_mapVolume = GetVolumeMap();
            m_timer = new Timer(CHECK_INTERVAL);
            m_timer.Enabled = true;
            m_timer.Elapsed += Check;
            m_isLoaded = true;
        }

        public override void Unload()
        {
            m_isLoaded = false;
        }

        public override Dictionary<string, object> GetVolumeMap()
        {
            Dictionary<string, object> mapVolume = new Dictionary<string, object>();
            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                mapVolume.Add(di.Name, GetVolumeProperty(di));
            }
            return mapVolume;
        }

        public override StoragePropertyStruct GetVolumeProperty(object volume)
        {
            StoragePropertyStruct properties = new StoragePropertyStruct();
            DriveInfo di = volume as DriveInfo;
            properties.VolumeName = di.Name;
            properties.VolumeTotalSize = di.TotalSize;
            properties.VolumeFreeSize = di.TotalFreeSpace;
            properties.VolumeState = GetVolumeState(di);
            return properties;
        }

        private VOLUME_STATE GetVolumeState(DriveInfo di)
        {
            return (di.TotalSize == 0) ? VOLUME_STATE.VOLUME_STATE_ERROR : VOLUME_STATE.VOLUME_STATE_NORMAL;
        }

        private bool UpdateVolumes()
        {
            if (null == m_mapVolume)
            {
                return false;
            }

            Dictionary<string, object> mapVolume = GetVolumeMap();
            Dictionary<string, object> appended = GetPositiveException(m_mapVolume, mapVolume);
            Dictionary<string, object> removed = GetNegativeException(m_mapVolume, mapVolume);

            foreach (KeyValuePair<string, object> volume in removed)
            {
                STORAGE_EVENT evt = STORAGE_EVENT.VOLUME_OLD_REMOVED;
                base.NotifyStorageProperties(evt, (StoragePropertyStruct)volume.Value);
            }
            
            foreach (KeyValuePair<string, object> volume in appended)
            {
                STORAGE_EVENT evt = STORAGE_EVENT.VOLUME_NEW_APPENDED;
                base.NotifyStorageProperties(evt, (StoragePropertyStruct)volume.Value);
            }

            foreach (string key in GetIntersectionKeys(m_mapVolume, mapVolume))
            {
                StoragePropertyStruct properties_old = (StoragePropertyStruct)m_mapVolume[key];
                StoragePropertyStruct properties_new = (StoragePropertyStruct)mapVolume[key];
                if (properties_old.VolumeTotalSize != properties_new.VolumeTotalSize)
                {
                    STORAGE_EVENT evt = STORAGE_EVENT.VOLUME_TOTALSIZE_CHANGED;
                    base.NotifyStorageProperties(evt, properties_new);
                }
                if (properties_old.VolumeFreeSize != properties_new.VolumeFreeSize)
                {
                    STORAGE_EVENT evt = STORAGE_EVENT.VOLUME_TOTALSIZE_CHANGED;
                    base.NotifyStorageProperties(evt, properties_new);
                }
            }

            m_mapVolume = mapVolume;
            return true;
        }

        private void Check(Object obj, ElapsedEventArgs args)
        {
            DateTime checkDT = args.SignalTime;
            if (!UpdateVolumes())
            {
                base.NotifyStorageProperties(STORAGE_EVENT.STORAGE_NOT_MONITOR, new StoragePropertyStruct());
            }
        }
    }
}
