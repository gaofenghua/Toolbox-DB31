using System;
using System.Collections.Generic;

namespace Toolbox_DB31
{
    public abstract class StorageManager : IStorageManager, IDisposable
    {
        protected STORAGE_MANUFACTURER m_storageOwner = STORAGE_MANUFACTURER.MANUFACTURER_UNKNOWN;
        protected bool m_isLoaded = false;
        protected List<string> m_listVolumeName = null;
        public event EventHandler<StorageEventArgs> NotificationReceived = delegate { };

        public StorageManager()
        {
            m_listVolumeName = new List<string>();
            this.Load();
        }

        public void Dispose()
        {
            this.Unload();
            this.m_listVolumeName = null;
        }

        public void CheckStorageProperties(string volume)
        {
            NotificationReceived(this, new StorageEventArgs(m_storageOwner, GetVolumeProperty(volume)));
        }

        public abstract void Load();
        public abstract void Unload();
        public abstract List<string> GetVolumeNameList();
        public abstract bool GetVolumeSize(string volume, out long total_size, out long free_size);
        public abstract VOLUME_STATE GetVolumeState(string volume);

        public StoragePropertyStruct GetVolumeProperty(string volume)
        {
            StoragePropertyStruct properties = new StoragePropertyStruct();
            if (!this.m_listVolumeName.Contains(volume))
            {
                properties.VolumeName = volume;
                long total = 0, free = 0;
                GetVolumeSize(volume, out total, out free);
                properties.VolumeTotalSize = total;
                properties.VolumeFreeSize = free;
                properties.VolumeState = GetVolumeState(volume);
            }
            return properties;
        }
    }
}
