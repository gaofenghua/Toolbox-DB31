using System;
using System.Collections.Generic;

namespace Toolbox_DB31
{
    public interface IStorageManager
    {
        List<string> GetVolumeNameList();
        bool GetVolumeSize(string volume, out long total_size, out long free_size);
        VOLUME_STATE GetVolumeState(string volume);
    }
}
