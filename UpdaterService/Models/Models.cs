using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UpdaterService.Models
{
    public class LocalSystemInfo
    {
        public OsType OsType { get; set; }
        public string OsName { get; set; }
        public string ComputerName { get; set; }
        public string CpuModel { get; set; }
        /// <summary>
        /// ram size in MB
        /// </summary>
        public long RamSize { get; set; }
        public long FreeRamSize { get; set; }
        public string DisplayResolution { get; set; }



        public double CpuTemperature { get; set; }
        public float CpuUsagePercent { get; set; }
        /// <summary>
        /// Hdd Space In GB
        /// </summary>
        public int HddTotalSpace { get; set; }
        /// <summary>
        /// Hdd Free Space In GB
        /// </summary>
        public int HddFreeSpace { get; set; }
        public DateTime ClientTime { get; set; }
        public string IpAddress { get; set; }
        public bool DaylightSaving { get; set; }
        public string GateWay { get; set; }
        public double HddTemperature { get; set; }
        public double CpuFanRPM { get; set; }
        public double FanRPM { get; set; }


    }


    public class AppRequirementSystemInfo
    {
        public int MinimumRam { get; set; }

        /// <summary>
        /// these computers can update the app even if minimum requirement will not compatible
        /// </summary>
        public List<string> ComputerNameExceptions;


        public bool Validate(LocalSystemInfo localSystemInfo,out string er)
        {
            er = "";
            if (ComputerNameExceptions.Any(x => x == localSystemInfo.ComputerName)) return true;
            if (localSystemInfo.RamSize < MinimumRam) {  er = "ramError"; return false;}
            return true;
        }
    }
}