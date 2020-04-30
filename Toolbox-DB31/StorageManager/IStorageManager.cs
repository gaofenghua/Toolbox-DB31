using System;
using System.Collections.Generic;

namespace Toolbox_DB31
{
    public interface IStorageManager
    {
        Dictionary<string, object> GetVolumeMap();
        bool GetVolumeSize(Dictionary<string, object> mapVolume, string volume, out long total_size, out long free_size);
        VOLUME_STATE GetVolumeState(Dictionary<string, object> mapVolume, string volume);
    }
}
