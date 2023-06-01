using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.AppCode
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
		public bool CreateBackup { get; set; }

		public ClientParams()
		{
			CreateBackup = true;
			LogToTxtFile = true;
		}


	}
}
