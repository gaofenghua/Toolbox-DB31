using System;
using System.Collections.Generic;
using System.IO;

namespace Toolbox_DB31
{
    public class GeneralStorageManager : StorageManager
    {
        private Dictionary<string, DriveInfo> m_mapVolume = null;

        public GeneralStorageManager()
        {
            m_storageOwner = STORAGE_MANUFACTURER.MANUFACTURER_UNKNOWN;
        }

        public override void Load()
        {
            m_mapVolume = new Dictionary<string, DriveInfo>();
            m_isLoaded = true;
        }

        public override void Unload()
        {
            m_isLoaded = false;
            m_mapVolume = null;
        }

        public override List<string> GetVolumeNameList()
        {
            if (UpdateVolumes())
            {
                return m_listVolumeName;
            }
            else
            {
                return null;
            }
        }

        public override bool GetVolumeSize(string volume, out long total_size, out long free_size)
        {
            if (UpdateVolumes() && (m_mapVolume.ContainsKey(volume)))
            {
                total_size = m_mapVolume[volume].TotalSize;
                free_size = m_mapVolume[volume].TotalFreeSpace;
                return true;
            }
            else
            {
                total_size = free_size = -1;
                return false;
            }
        }

        public override VOLUME_STATE GetVolumeState(string volume)
        {
            if (UpdateVolumes())
            {
                return m_listVolumeName.Contains(volume) ? VOLUME_STATE.STATE_NORMAL : VOLUME_STATE.STATE_ERROR;
            }
            else
            {
                return VOLUME_STATE.STATE_UNKNOWN;
            }
        }

        private bool UpdateVolumes()
        {
            if ((null == m_listVolumeName) || (null == m_mapVolume))
            {
                return false;
            }

            m_listVolumeName.Clear();
            m_mapVolume.Clear();
            foreach (DriveInfo di in DriveInfo.GetDrives())
            {
                m_listVolumeName.Add(di.Name);
                m_mapVolume.Add(di.Name, di);
            }
            return true;
        }
    }
}
