using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Updater.Models
{
	public class ClientParams
	{
		/// <summary>
		/// Server url 
		/// </summary>
		public string ServerUrl { get; set; }
		/// <summary>
		/// App Name
		/// </summary>
		public string TargetAppName { get; set; }
		/// <summary>
		/// updater will save logs into text file
		/// </summary>
		public bool LogToTxtFile { get; set; }
		public bool AutoUpdate { get; set; }
        /// <summary>
        /// get a backup from old files before replace with updated files
        /// </summary>
		public bool CreateBackup { get; set; }
        /// <summary>
        /// force to remove last update parts and redownload them
        /// </summary>
        public bool Force { get; set; }
        /// <summary>
        /// Determines Time to sleep between download parts (in milliseconds)
        /// </summary>
        public  int LatencyBetweenParts { get; set; }

        public ClientParams()
		{
			CreateBackup = true;
			LogToTxtFile = true;
            Force = false;
            LatencyBetweenParts = 50;
        }


	}
}
