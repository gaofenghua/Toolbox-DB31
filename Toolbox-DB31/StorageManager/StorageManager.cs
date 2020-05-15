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

        public abstract StoragePropertyStruct GetVolumeProperty(object volume);

        public abstract void Load();
        public abstract void Unload();
        public abstract Dictionary<string, object> GetVolumeMap();

        public static Dictionary<T1, T2> GetPositiveException<T1, T2>(Dictionary<T1, T2> old_map, Dictionary<T1, T2> new_map)
        {
            Dictionary<T1, T2> appended = new Dictionary<T1, T2>();
            var appendedKeys = new_map.Keys.Except(old_map.Keys);
            var a = from dict in new_map where appendedKeys.Contains(dict.Key) select dict;
            foreach (KeyValuePair<T1, T2> p in a)
            {
                appended.Add(p.Key, p.Value);
            }
            return appended;
        }

        public static Dictionary<T1, T2> GetNegativeException<T1, T2>(Dictionary<T1, T2> old_map, Dictionary<T1, T2> new_map)
        {
            Dictionary<T1, T2> removed = new Dictionary<T1, T2>();
            var removedKeys = old_map.Keys.Except(new_map.Keys);
            var a = from dict in old_map where removedKeys.Contains(dict.Key) select dict;
            foreach (KeyValuePair<T1, T2> p in a)
            {
                removed.Add(p.Key, p.Value);
            }
            return removed;
        }

        public static List<T1> GetIntersectionKeys<T1, T2>(Dictionary<T1, T2> old_map, Dictionary<T1, T2> new_map)
        {
            return old_map.Keys.ToList().Intersect(new_map.Keys.ToList()).ToList();
        }
    }
}
