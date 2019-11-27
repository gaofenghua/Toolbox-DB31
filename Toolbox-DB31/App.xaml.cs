using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using Toolbox_DB31.Classes;

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
        public static ObservableCollection<Camera_Model> g_CameraList = new ObservableCollection<Camera_Model>();

        public static Main_ViewModel g_Main_ViewModel = null;
    }
        
}
