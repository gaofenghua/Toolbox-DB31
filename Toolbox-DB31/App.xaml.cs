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
using System.IO;

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

        public static AVMSAdapter g_VMS_Adapter = null;
        public static List<StorageManager> g_Storage_List = null;

        public static readonly object LogFile_Lock = new object();
        public static void WriteLog(string cont)
        {
            string path = System.Windows.Forms.Application.StartupPath.ToString() + @"\" + "toolbox.log";
            bool isAppend = true;

            lock (LogFile_Lock)
            {
                using (StreamWriter sw = new StreamWriter(path, isAppend, System.Text.Encoding.UTF8))
                {
                    //sw.WriteLine(DateTime.Now);
                    cont = DateTime.Now + "---" + cont;
                    sw.WriteLine(cont);
                    sw.Close();
                }
            }
        }
    }
        
}
