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
        public StoragePropertyStruct StorageProperties { get; }

        public StorageEventArgs(STORAGE_MANUFACTURER owner, StoragePropertyStruct properties)
        {
            StorageOwner = owner;
            StorageProperties = properties;
        }
    }

    public enum STORAGE_MANUFACTURER
    {
        MANUFACTURER_UNKNOWN = 0,
        MANUFACTURER_PROMISE
    }

    public enum VOLUME_STATE
    {
        STATE_UNKNOWN = 0,
        STATE_NORMAL,
        STATE_ERROR
    }
}
