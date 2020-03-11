using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Seer.DeviceModel.Client;
using Toolbox_DB31.Classes;

namespace Toolbox_DB31.AVMS_Adapter
{
    public class DeviceConfiguration
    {
        public string AgentId { get; private set; } = string.Empty;
        public string StartIp { get; private set; } = string.Empty;
        public string EndIp { get; private set; } = string.Empty;

        public DeviceConfiguration()
        {
            //
        }

        public DeviceConfiguration(string agentId, string startIp, string endIp)
        {
            SetData(agentId, startIp, endIp);
        }

        public void SetData(string agentId, string startIp, string endIp)
        {
            AgentId = agentId;
            StartIp = startIp;
            EndIp = endIp;
        }
    }

    public class DeviceSummary
    {
        private static string cfgDirectory = string.Empty;
        private static string cfgFileName = string.Empty;
        private static List<DeviceConfiguration> devList = new List<DeviceConfiguration>();
        private const string FILE_NAME = "Configuration";
        private const string FILE_FORMAT = ".csv";

        public static string CfgFilePath
        {
            get
            {
                string path = cfgDirectory + @"\" + cfgFileName + FILE_FORMAT;
                return CheckFileCondition(path, FILE_FORMAT) ? path : string.Empty;
            }
            set
            {
                if (CheckFileCondition(value, FILE_FORMAT))
                {
                    cfgDirectory = Path.GetDirectoryName(value);
                    cfgFileName = Path.GetFileNameWithoutExtension(value);
                }
                else
                {
                    cfgDirectory = System.Windows.Forms.Application.StartupPath.ToString();
                    cfgFileName = FILE_NAME;
                }
            }
        }

        private static void PrintLog(string text)
        {
            Trace.WriteLine(text);
        }

        public static bool ImportConfiguration()
        {
            if (null == devList)
            {
                return false;
            }
            //ImportDummyData();
            ImportFromCSV();
            return devList.Count > 0;
        }

        public static void UpdateTable(Dictionary<uint, CCamera> camList)
        {
            if ((0 == devList.Count) && !ImportConfiguration())
            {
                PrintLog("Device configurations are not available!");
                return;
            }

            foreach (KeyValuePair<uint, CCamera> item in camList)
            {
                string camIp = item.Value.IPAddress;
                long range = 0;
                long offset = 0;
                var result = devList.Where(deviceConfig => ((range = (ConvertIpToLong(deviceConfig.EndIp) - ConvertIpToLong(deviceConfig.StartIp))) >= 0) && (range <= 127)
                                                    && ((offset = (ConvertIpToLong(camIp) - ConvertIpToLong(deviceConfig.StartIp))) >= 0) && (offset <= range)).ToList();
                if (0 < result.Count)
                {
                    DeviceConfiguration df = result.First();
                    Global.g_CameraList.Add(new Camera_Model() { AgentID = df.AgentId, ChannelNumber = (int)(ConvertIpToLong(camIp) - ConvertIpToLong(df.StartIp)), CameraID = (int)item.Key, Name = item.Value.Name, Status = "在线", IsSelected = false });
                }
            }
        }

        private static void ImportDummyData()
        {
            devList.Clear();
            devList.Add(new DeviceConfiguration("0010123033030", "192.168.77.100", "192.168.77.227"));
            devList.Add(new DeviceConfiguration("0010123033031", "192.168.77.227", "192.168.78.10"));
        }

        private static bool ImportFromCSV()
        {
            try
            {
                if (string.Empty == CfgFilePath)
                {
                    PrintLog("Invalid file : " + CfgFilePath);
                    return false;
                }

                Stream stream = File.Open(CfgFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                StreamReader sr = new StreamReader(stream);

                string content = sr.ReadToEnd();
                stream.Close();
                if (string.Empty == content)
                {
                    PrintLog("Empty file : " + CfgFilePath);
                    return false;
                }

                devList.Clear();
                string[] lines = content.Split('\n');
                foreach (string line in lines)
                {
                    if ((string.Empty == line) || ("\r" == line))
                    {
                        PrintLog(String.Format("Invalid line [{0}], skip...\n", line.Trim()));
                        continue;
                    }

                    DeviceConfiguration devConfig = new DeviceConfiguration();
                    bool isOK = Process(line, ',', ref devConfig);
                    if (!isOK)
                    {
                        PrintLog(String.Format("Process failed with line [{0}], skip...\n", line.Trim()));
                        continue;
                    }
                    else
                    {
                        var list = devList.Where(dc => dc.AgentId == devConfig.AgentId).ToList();
                        if (0 == list.Count)
                        {
                            devList.Add(devConfig);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                PrintLog(String.Format("Error importing file \"{0}\" : {1}", CfgFilePath, ex.ToString()));
                return false;
            }
        }

        private static bool Process(string data, char separator, ref DeviceConfiguration dc)
        {
            string[] info = data.Trim().Split(separator);
            string log = String.Format("Parse data[{0}] into acap camera : ", data.Trim());

            if (3 != info.Length)
            {
                log += "Invalid device structure\n";
                PrintLog(log);
                return false;
            }

            if ((string.Empty == info[0]) || (string.Empty == info[1]) || (string.Empty == info[2]))
            {
                log += "One empty value at least for device info\n";
                PrintLog(log);
                return false;
            }

            if ((!ValidateIPAddress(info[1])) || (!ValidateIPAddress(info[2])))
            {
                log += String.Format("Invalid ip address data - start = {0}, end = {1}\n", info[1], info[2]);
                PrintLog(log);
                return false;
            }
            string ip = info[0];
            log += String.Format("ip = {0} with\n", info[0]);
            dc.SetData(info[0], info[1], info[2]);

            PrintLog(log);
            return true;
        }

        private static bool CheckFileCondition(string sFilename, string sFileFormat)
        {
            if (!File.Exists(sFilename))
            {
                PrintLog("File does not exist: " + sFilename);
                return false;
            }

            if (sFileFormat.ToLower() != Path.GetExtension(sFilename).ToLower())
            {
                PrintLog("Invalid file format : " + sFilename);
                return false;
            }

            return true;
        }

        public static bool ValidateIPAddress(string data)
        {
            Regex regex = new Regex(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
            return (data != "" && regex.IsMatch(data.Trim())) ? true : false;
        }

        public static long ConvertIpToLong(string sIp)
        {
            long iIp = 0;
            string[] ips = sIp.Split('.');
            iIp = long.Parse(ips[0]) << 0x18 | long.Parse(ips[1]) << 0x10 | long.Parse(ips[2]) << 0x8 | long.Parse(ips[3]);
            return iIp;
        }

        public static string ConvertLongToIp(long iIp)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(iIp >> 0x18 & 0xff).Append(".");
            sb.Append(iIp >> 0x10 & 0xff).Append(".");
            sb.Append(iIp >> 0x8 & 0xff).Append(".");
            sb.Append(iIp & 0xff);
            return sb.ToString();
        }
    }

}
