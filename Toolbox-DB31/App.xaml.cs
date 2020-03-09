using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Threading;
using Toolbox_DB31.Classes;
using Toolbox_DB31.DB31_Adapter;
using Toolbox_DB31.AVMS_Adapter;

namespace Toolbox_DB31
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
      
    }

    public static class Global
    {
        public static Mutex g_CameraList_Mutex = new Mutex();
        public static ObservableCollection<Camera_Model> g_CameraList = new ObservableCollection<Camera_Model>();

        public static Main_ViewModel g_Main_ViewModel = null;

        public static DB31_User g_User = new DB31_User();

        public static AVMSAdapter g_VMS_Adapter = new AVMSAdapter();
    }
        
}
