using System;
using System.Collections.Generic;
using System.Linq;

namespace Toolbox_DB31
{
    public abstract class StorageManager : IStorageManager, IDisposable
    {
        protected STORAGE_MANUFACTURER m_storageOwner = STORAGE_MANUFACTURER.MANUFACTURER_UNKNOWN;
        protected bool m_isLoaded = false;
        protected Dictionary<string, object> m_mapVolume = null;
        public event EventHandler<StorageEventArgs> NotificationReceived = delegate { };

        public StorageManager()
        {
            this.m_mapVolume = new Dictionary<string, object>();
            this.Load();
        }

        public void Dispose()
        {
            this.Unload();
            this.m_mapVolume = null;
        }

        public void NotifyStorageProperties(STORAGE_EVENT evt, StoragePropertyStruct volumeProperties)
        {
            NotificationReceived(this, new StorageEventArgs(m_storageOwner, evt, volumeProperties));
        }

        public StoragePropertyStruct GetVolumeProperty(Dictionary<string, object> mapVolume, string volume)
        {
            StoragePropertyStruct properties = new StoragePropertyStruct();
            if (mapVolume.ContainsKey(volume))
            {
                properties.VolumeName = volume;
                long total = 0, free = 0;
                GetVolumeSize(mapVolume, volume, out total, out free);
                properties.VolumeTotalSize = total;
                properties.VolumeFreeSize = free;
                properties.VolumeState = GetVolumeState(mapVolume, volume);
            }
            return properties;
        }

        public abstract void Load();
        public abstract void Unload();
        //public abstract List<string> GetVolumeNameList();
        public abstract Dictionary<string, object> GetVolumeMap();
        public abstract bool GetVolumeSize(Dictionary<string, object> mapVolume, string volume, out long total_size, out long free_size);
        public abstract VOLUME_STATE GetVolumeState(Dictionary<string, object> mapVolume, string volume);

        public static Dictionary<T1, T2> GetPositiveException<T1, T2>(Dictionary<T1, T2> old_map, Dictionary<T1, T2> new_map)
        {
            return new_map.Keys.Except(old_map.Keys) as Dictionary<T1, T2>;
        }

        public static Dictionary<T1, T2> GetNegativeException<T1, T2>(Dictionary<T1, T2> old_map, Dictionary<T1, T2> new_map)
        {
            return old_map.Keys.Except(new_map.Keys) as Dictionary<T1, T2>;
        }

        public static List<T1> GetIntersectionKeys<T1, T2>(Dictionary<T1, T2> old_map, Dictionary<T1, T2> new_map)
        {
            return old_map.Keys.ToList().Intersect(new_map.Keys.ToList()).ToList();
        }
    }
}
