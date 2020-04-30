using System;

namespace Toolbox_DB31
{
    public struct StoragePropertyStruct
    {
        public string VolumeName { get; set; }
        public long VolumeTotalSize { get; set; }
        public long VolumeFreeSize { get; set; }
        public VOLUME_STATE VolumeState { get; set; }
    }

    public class StorageEventArgs : EventArgs
    {
        public STORAGE_MANUFACTURER StorageOwner { get; }
        public STORAGE_EVENT StorageEvent { get; }
        public StoragePropertyStruct StorageProperties { get; }

        public StorageEventArgs(STORAGE_MANUFACTURER owner, STORAGE_EVENT evt, StoragePropertyStruct properties)
        {
            StorageOwner = owner;
            StorageEvent = evt;
            StorageProperties = properties;
        }
    }

    public enum STORAGE_MANUFACTURER
    {
        MANUFACTURER_UNKNOWN = 0,
        MANUFACTURER_PROMISE
    }

    public enum STORAGE_EVENT
    {
        STORAGE_NOT_MONITOR = 0,
        VOLUME_NEW_APPENDED,
        VOLUME_OLD_REMOVED,
        VOLUME_TOTALSIZE_CHANGED,
        VOLUME_FREESIZE_CHANGED,
    }

    public enum VOLUME_STATE
    {
        VOLUME_NOT_EXIST = 0,
        VOLUME_STATE_UNKNOWN,
        VOLUME_STATE_NORMAL,
        VOLUME_STATE_ERROR,
    }
}
