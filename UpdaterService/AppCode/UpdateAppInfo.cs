using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using UpdaterService.Models;

namespace UpdaterService
{
    [DataContract]
    public class UpdateAppInfo
    {
        [DataMember]
        public int AppId { get; set; }

        [DataMember]
        public string Version { get; set; }

		[DataMember]
		public string NewFeatures { get; set; }

		[DataMember]
        public UpdateVersionPriority updatePriority { get; set; }
		[DataMember]
		public bool extractZipFiles { get; set; }

        [DataMember]
        public List<UpdateFileInfo> files { get; set; }


	}
	[DataContract]
    public class UpdateFileInfo
    {
		/// <summary>
		/// Releative Full FileName In Server And Client
		/// </summary>
		[DataMember]
        public string FileName { get; set; }
        [DataMember]
        public DateTime CreationTime { get; set; }
        [DataMember]
        public DateTime ModifiedTime { get; set; }
        [DataMember]
        public string AssemblyVersion { get; set; }
        [DataMember]
        public long Length { get; set; }
        [DataMember]
        public bool IsZipped { get; set; }
        [DataMember]
        public int StartPartId { get; set; }
        [DataMember]
        public int EndPartId { get; set; }
		/// <summary>
		/// will be overwrited if exists in the client app
		/// </summary>
		[DataMember]
		public bool Overwrite { get; set; }
		/// <summary>
		/// will be deleted if exists in the client app
		/// </summary>
		[DataMember]
		public bool Delete { get; set; }

	}
}