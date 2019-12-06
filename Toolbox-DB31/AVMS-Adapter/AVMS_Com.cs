using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Seer;
using Seer.SDK;
using Seer.BaseLibCS;
using Seer.DeviceModel.Client;
using Seer.BaseLibCS.Communication;
using System.Diagnostics;
using Toolbox_DB31.Classes;

using System.IO;
using System.Drawing;
using System.Drawing.Imaging;


namespace Toolbox_DB31.AVMS_Adapter
{
    class AVMS_Com
    {
        private SdkFarm m_farm = null;
        public AVMS_Com()
        {
            CNetworkAddress address = new CNetworkAddress("192.168.77.211");
            m_farm = new SdkFarm(address, "admin", Utils.EncodeString("admin"));
            m_farm.DeviceModelRefreshTrigger = Seer.FarmLib.Client.CFarm.DeviceAutoRefreshTrigger.AnyChange;

            string sret = m_farm.Connect();

            CDeviceManager deviceManager = m_farm.DeviceManager;
            deviceManager.DataLoadedEvent += new EventHandler<EventArgs>(DeviceManager_DataLoadedEvent);
            deviceManager.Refresh();

            foreach (CCamera cam in m_farm.DeviceManager.GetAllCameras())
            {
                Trace.WriteLine(cam.CameraId.ToString() + cam.ToString() + cam.Name);

                Global.g_CameraList.Add(new Camera_Model() { AgentID =cam.CameraId.ToString(), Name = cam.Name, IsSelected = true });
            }

            //using (ServerConnectionManager scm = ServerConnectionManager.CreateManager(Utils.ToEndPoints(address), new Credentials("admin", Utils.EncodeString("admin"))))
            //using (Seer.BaseLibCS.SeerWS.Signals ws = scm.GetWebServiceProxy<Seer.BaseLibCS.SeerWS.Signals>())
            //{
            //    try
            //    {
            //        string webPath;
            //        int decoration = CameraViewSettings.DECOR_DEFAULT + Utils.GetViewPrivateVideoDecoration(true);
            //        byte[] byteJpg = ws.GetJPEGImage3("admin", Utils.EncodeString("admin"), 24, DateTime.Now.ToUniversalTime(), 0, false, decoration, out webPath);
            //        using (MemoryStream ms = new MemoryStream(byteJpg, false))
            //        using (Image image = Image.FromStream(ms))
            //        {
            //            image.Save("d:\\test.jpg", ImageFormat.Jpeg);
            //            Console.WriteLine(string.Format("Image was saved to: {0}", "d:\test.jpg"));
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(string.Format("There was a problem getting/saving the image: {0}", ex.ToString()));
            //    }
            //}

            Seer.BaseLibCS.SeerWS.Signals ws = m_farm.GetWebServiceProxy<Seer.BaseLibCS.SeerWS.Signals>();
            string webPath;
            int decoration = CameraViewSettings.DECOR_DEFAULT + Utils.GetViewPrivateVideoDecoration(true);
            byte[] byteJpg = ws.GetJPEGImage3("admin", Utils.EncodeString("admin"), 9, new DateTime(2019,11,20,11,0,0).ToUniversalTime(), 0, false, decoration, out webPath);
            MemoryStream ms = new MemoryStream(byteJpg, false);
            Image image = Image.FromStream(ms);
            image.Save("d:\\test.jpg", ImageFormat.Jpeg);
            Console.WriteLine(string.Format("Image was saved to: {0}", "d:\test.jpg"));
            
        }

        void DeviceManager_DataLoadedEvent(object sender, EventArgs e)
        {

        }
    }
}
