using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.AppCode
{
	[Flags]
	public enum FileChangeBy
	{ Length = 2, ModifiedDate = 4, AssemplyVersion = 8 }
	public enum InitUpdateState
	{ 
	//App Would not update yet
	NoUpdateFound = 0,
	//App Requires Update
	UpdateRequires,
	//.....
	ErrorConnectingServer,
	//App Is Update To Latest Version Now
	AppIsUpdateNow,
	//Update Must Resume
	ResumeUpdate,
	//
	ErrorServerSide,
	UpdateAbort
	}
	public enum LogTypes
	{
	Success = 0,Info,Warning,Error
	}
	

}
