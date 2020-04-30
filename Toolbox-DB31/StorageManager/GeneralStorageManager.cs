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
        private const int CHECK_INTERVAL = 65 * 1000;

        public GeneralStorageManager()
        {
            m_storageOwner = STORAGE_MANUFACTURER.MANUFACTURER_UNKNOWN;
        }

        public override void Load()
        {
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
                mapVolume.Add(di.Name, di);
            }
            return mapVolume;
        }

        public override bool GetVolumeSize(Dictionary<string, object> mapVolume, string volume, out long total_size, out long free_size)
        {
            if (mapVolume.ContainsKey(volume))
            {
                {
                    GetVolumeSize((DriveInfo)m_mapVolume[volume], volume, out total_size, out free_size);
                    return true;
                }
            }
            else
            {
                total_size = free_size = -1;
                return false;
            }

            //if (UpdateVolumes() && (m_mapVolume.ContainsKey(volume)))
            //{
            //    GetVolumeSize((DriveInfo)m_mapVolume[volume], volume, out total_size, out free_size);
            //    return true;
            //}
            //else
            //{
            //    total_size = free_size = -1;
            //    return false;
            //}
        }

        public override VOLUME_STATE GetVolumeState(Dictionary<string, object> mapVolume, string volume)
        {
            return m_mapVolume.ContainsKey(volume) ? GetVolumeState((DriveInfo)m_mapVolume[volume], volume) : VOLUME_STATE.VOLUME_NOT_EXIST;

            //if (UpdateVolumes())
            //{
            //    return m_mapVolume.ContainsKey(volume) ? GetVolumeState((DriveInfo)m_mapVolume[volume], volume) : VOLUME_STATE.VOLUME_NOT_EXIST;
            //}
            //else
            //{
            //    return VOLUME_STATE.VOLUME_NOT_MONITOR;
            //}
        }

        public void GetVolumeSize(DriveInfo di, string volume, out long total_size, out long free_size)
        {
            total_size = di.TotalSize;
            free_size = di.TotalFreeSpace;
        }

        private VOLUME_STATE GetVolumeState(DriveInfo di, string volume)
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
                StoragePropertyStruct properties = GetVolumeProperty(removed, volume.Key);
                base.NotifyStorageProperties(evt, properties);
            }
            
            foreach (KeyValuePair<string, object> volume in appended)
            {
                STORAGE_EVENT evt = STORAGE_EVENT.VOLUME_NEW_APPENDED;
                StoragePropertyStruct properties = GetVolumeProperty(appended, volume.Key);
                base.NotifyStorageProperties(evt, properties);
            }

            foreach (string key in GetIntersectionKeys(m_mapVolume, mapVolume))
            {
                DriveInfo Di_old = m_mapVolume[key] as DriveInfo;
                DriveInfo Di_new = mapVolume[key] as DriveInfo;
                if (Di_old.TotalSize != Di_new.TotalSize)
                {
                    STORAGE_EVENT evt = STORAGE_EVENT.VOLUME_TOTALSIZE_CHANGED;
                    StoragePropertyStruct properties = GetVolumeProperty(mapVolume, key);
                    base.NotifyStorageProperties(evt, properties);
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

        //private bool IsEqualList<T>(List<T> old_list, List<T> new_list, out List<T> appended, out List<T> removed)
        //{
        //    removed = old_list.Except(new_list).ToList();
        //    appended = new_list.Except(old_list).ToList();
        //    return (0 == removed.Count) && (0 == appended.Count);
        //}

        //private bool IsEqualKey<T1, T2>(Dictionary<T1, T2> old_map, Dictionary<T1, T2> new_map, out Dictionary<T1, T2> appended, out Dictionary<T1, T2> removed)
        //{
        //    removed = old_map.Keys.Except(new_map.Keys) as Dictionary<T1, T2>;
        //    appended = new_map.Keys.Except(old_map.Keys) as Dictionary<T1, T2>;
        //    return (0 == removed.Count) && (0 == appended.Count);
        //}
    }
}
