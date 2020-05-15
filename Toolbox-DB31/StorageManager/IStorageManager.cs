using System;
using System.Collections.Generic;

namespace Toolbox_DB31
{
    public interface IStorageManager
    {
        Dictionary<string, object> GetVolumeMap();
    }
}
