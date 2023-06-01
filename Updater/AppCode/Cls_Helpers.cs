using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Updater.UpdaterServiceReference;

namespace Updater.AppCode
{
    public static class Cls_Helpers
    {
        //a.Win32_ComputerSystem

        //b.Win32_DiskDrive

        //c.Win32_OperatingSystem

        //d.Win32_Processor

        //e.Win32_ProgramGroup

        //f.Win32_SystemDevices

        //g.Win32_StartupCommand


        //		Win32_ComputerSystem	****************
        //9-caption(computer name)
        //27-manufacture = dell
        //28 - model(latitude 4214)
        //29-Name(computer name)
        //32-NumberOfLogicalProcessors 8
        //33-NumberOfProcessors 1
        //60-TotalPhysicalMemory
        //61-UserName



        //Win32_OperatingSystem	****************
        //3-Caption(windows 10 enterprise)
        //20-FreePhysicalMemory
        //23-InstallDate
        //35-NumberOfUsers
        //38-OSArchitecture
        //39-OSLanguage
        //46-PortableOperatingSystem
        //49-RegisteredUser
        //50-SerialNumber
        //60-TotalVirtualMemorySize



        //Win32_Processor	****************
        //1-Architecture
        //4-Caption
        //27-Manufacturer
        //29-Name Intel(R) Core(TM) i7-3740QM CPU @ 2.70GHz
        //30-NumberOfCores
        public static ArrayList GetInformation(string qry)
        {
            ManagementObjectSearcher searcher;
            int i = 0;
            ArrayList arrayListInformationCollactor = new ArrayList();

            searcher = new ManagementObjectSearcher("SELECT * FROM " + qry);
            foreach (ManagementObject mo in searcher.Get())
            {
                i++;
                PropertyDataCollection searcherProperties = mo.Properties;
                foreach (PropertyData sp in searcherProperties)
                {
                    arrayListInformationCollactor.Add(sp);
                }
            }
            return arrayListInformationCollactor;
        }
        public static async Task<LocalSystemInfo> GetLocalSystemInfo()
        {
            return await Task.Run(new Func<LocalSystemInfo>(() =>
            {
                try
                {
                    //run cmd as administrator then run these commands to enable get hardware info

                    //lodctr /q
                    //lodctr /r
                    var arrayListWin32_ComputerSystem = new List<PropertyData>();

                    var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        PropertyDataCollection searcherProperties = mo.Properties;
                        foreach (PropertyData sp in searcherProperties)
                        {
                            arrayListWin32_ComputerSystem.Add(sp);
                        }
                    }

                    var arrayListWin32_OperatingSystem = new List<PropertyData>();
                    var searcher2 = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                    foreach (ManagementObject mo in searcher2.Get())
                    {
                        PropertyDataCollection searcherProperties = mo.Properties;
                        foreach (PropertyData sp in searcherProperties)
                        {
                            arrayListWin32_OperatingSystem.Add(sp);
                        }
                    }

                    var arrayListWin32_Processor = new List<PropertyData>();
                    var searcher3 = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    foreach (ManagementObject mo in searcher3.Get())
                    {
                        PropertyDataCollection searcherProperties = mo.Properties;
                        foreach (PropertyData sp in searcherProperties)
                        {
                            arrayListWin32_Processor.Add(sp);
                        }
                    }
                    var ret = new LocalSystemInfo()
                    {

                        ComputerName = arrayListWin32_ComputerSystem[9].Value.ToString(),
                        OsName = arrayListWin32_OperatingSystem[3].Value.ToString(),
                        RamSize = Convert.ToInt64(arrayListWin32_ComputerSystem[60].Value) / 1024 / 1024,
                        FreeRamSize = Convert.ToInt64(arrayListWin32_OperatingSystem[20].Value) / 1024 / 1024,
                        CpuModel = arrayListWin32_Processor[29].Value.ToString(),
                        CpuTemperature = GetCupTemperature(),
                        CpuUsagePercent = GetCupUsagePercent(),
                        CpuFanRPM = GetCPUFan()
                    };
                    return ret;
                }
                catch (Exception ex)
                {
                    return new LocalSystemInfo();
                }
            }));


        }

        static float GetCupUsagePercent()
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_PerfFormattedData_PerfOS_Processor");
            int counter = 0;
            double sum = 0;
            foreach (ManagementObject obj in searcher.Get())
            {
                counter++;
                var usage = obj["PercentProcessorTime"];
                var name = obj["Name"];
                sum += Convert.ToDouble(usage);
            }
            return (float)sum / counter;
        }
        static double GetCupTemperature()
        {
            double temperature = 0;
            string instanceName = "";
            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM MSAcpi_ThermalZoneTemperature");

                foreach (ManagementObject obj in searcher.Get())
                {
                    temperature = Convert.ToDouble(obj["CurrentTemperature"].ToString());
                    // Convert your value to celsius degrees
                    temperature = (temperature - 2732) / 10.0;
                    instanceName = obj["InstanceName"].ToString();
                }
            }
            catch (Exception ex) { }




            return temperature;
        }
        static float GetCPUFan()
        {

            return 0;
        }
        public static string Ext_EasyReadLength(this long FileLength)
        {
            if (FileLength < 921) return $"{FileLength} Bytes";
            double d1 = (double)FileLength / 1024;
            if (d1 < 921) return $"{Math.Round(d1, 1)} KB";
            d1 = (double)d1 / 1024;
            if (d1 < 921) return $"{Math.Round(d1, 1)} MB";
            d1 = (double)d1 / 1024;
            if (d1 < 921) return $"{Math.Round(d1, 1)} GB";
            d1 = (double)d1 / 1024;
            return $"{Math.Round(d1, 1)} TB";
        }
        /// <summary>Serializes an object of type T in to an xml string</summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="obj">Object to serialize</param>
        /// <returns>A string that represents Xml, empty otherwise</returns>
        public static string Ext_XmlSerialize<T>(this T obj) //where T : class, new()
        {
            if (obj == null) throw new ArgumentNullException("obj");

            var serializer = new XmlSerializer(typeof(T));
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, obj);
                return writer.ToString();
            }
        }



        /// <summary>Deserializes an xml string in to an object of Type T</summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="xml">Xml as string to deserialize from</param>
        /// <returns>A new object of type T is successful, null if failed</returns>
        public static T Ext_XmlDeserialize<T>(this string xml) where T : class, new()
        {
            if (xml == null) throw new ArgumentNullException("xml");

            var serializer = new XmlSerializer(typeof(T));
            using (var reader = new StringReader(xml))
            {
                try
                {
                    return (T)serializer.Deserialize(reader);
                }
                catch (Exception ex)
                {
                    return null;
                } // Could not be deserialized to this type.
            }
        }
        public static string Ext_ToFilename(this DateTime d1)
        {
            return d1.ToString().Replace(' ', '_').Replace('/', '.').Replace(':', '.');
        }
        public static string Ext_Message(this Exception ex)
        {
            return ex.Message + (ex.InnerException == null ? "" : ",Inner Exception:" + ex.InnerException.Message);
        }
    }
}
