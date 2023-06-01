using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateServiceInitializer.Models
{
    public enum UpdateVersionPriority
    {
        Low = 0, Normal, High, VeryHigh
    }
    public class UpdateAppInfo
    {
        
        public int AppId { get; set; }

        
        public string Version { get; set; }

        
        public string NewFeatures { get; set; }

        
        public UpdateVersionPriority updatePriority { get; set; }
        
        public bool extractZipFiles { get; set; }

        
        public List<UpdateFileInfo> files { get; set; }


    }

    public class UpdateFileInfo
    {
        public string FileName { get; set; }
        
        public DateTime CreationTime { get; set; }
        
        public DateTime ModifiedTime { get; set; }
        
        public string AssemblyVersion { get; set; }
        
        public long Length { get; set; }
        
        public bool IsZipped { get; set; }
        
        public int StartPartId { get; set; }
        
        public int EndPartId { get; set; }
        /// <summary>
        /// will be overwrited if exists in the client app
        /// </summary>
        
        public bool Overwrite { get; set; }
        /// <summary>
        /// will be deleted if exists in the client app
        /// </summary>
        
        public bool Delete { get; set; }

    }



    public class AppRequirementSystemInfo
    {
        public int MinimumRam { get; set; }

        /// <summary>
        /// these computers can update the app even if minimum requirement will not compatible
        /// </summary>
        public List<string> ComputerNameExceptions;
        public AppRequirementSystemInfo()
        {
            ComputerNameExceptions = new List<string>();
        }
    }
}
