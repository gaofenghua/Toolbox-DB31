using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Mvvm;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Toolbox_DB31.Classes
{
    class Summary_ViewModel : ViewModelBase
    {
  
        public ObservableCollection<Camera_Model> CameraList { get; set; }

        public Summary_ViewModel()
        {
            //CameraList = new ObservableCollection<Camera_Model>();
            CameraList = Global.g_CameraList;
            CameraList.Add(new Camera_Model() { Name = "test",IsSelected=true });
            CameraList.Add(new Camera_Model() { Name = "test2", IsSelected = false });
        }
        
   
    }
}
