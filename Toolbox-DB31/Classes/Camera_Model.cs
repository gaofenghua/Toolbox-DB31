using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox_DB31.Classes
{
    public class Camera_Model
    {
        public string AgentID { get; set; }
        public int ChannelNumber { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public bool IsSelected { get; set; }
    }
}
