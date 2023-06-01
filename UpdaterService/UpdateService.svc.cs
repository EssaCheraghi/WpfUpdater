using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using UpdaterService.Models;
using Mahdyar_Library;

namespace UpdaterService
{
	// NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "UpdateService" in code, svc and config file together.
	// NOTE: In order to launch WCF Test Client for testing this service, please select UpdateService.svc or UpdateService.svc.cs at the Solution Explorer and start debugging.
	public class UpdateService : IUpdateService
	{
		static string[] UpdateDirectory;
		public string GetFileBlock(string JsonInput, out string error)
		{
			error = null;
			try
			{
				if (UpdateDirectory == null || UpdateDirectory.Length == 0)
					UpdateDirectory = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + @"Updates");

				var ClientObject = new
				{
					AppId = (int?)null,
					BlockNumber = (int?)null
				};
				ClientObject = JsonConvert.DeserializeAnonymousType(JsonInput, ClientObject);
				string direct = UpdateDirectory.FirstOrDefault(x => Path.GetFileName(x).Split('-')[0] == ClientObject.AppId.ToString());
				if (string.IsNullOrEmpty(direct)) throw new Exception($"No Directory Found In the server for AppId ={ClientObject.AppId}");
				if (!File.Exists(direct + $@"\parts\part_{ClientObject.BlockNumber}")) error = "Complete";
				else
				{
					return System.Convert.ToBase64String(File.ReadAllBytes(direct + $@"\parts\part_{ClientObject.BlockNumber}"));
				}
			}
			catch (Exception ex)
			{
				error = ex.Ext_GetFullMessage();
			}
			return null;
		}

		public bool? SelfUpdateNeeded(string JsonInput, out string error)
		{
			error = null;
			try
			{
				var ClientObject = new
				{
					SizeBytes = (int?)null,
					LatestUpdateDate = (DateTime?)null
				};
				ClientObject = JsonConvert.DeserializeAnonymousType(JsonInput, ClientObject);
				var fileinfo = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + @"Assets\Updater.exe");
				if (fileinfo.Length == ClientObject.SizeBytes)
					if (fileinfo.CreationTime == ClientObject.LatestUpdateDate)
						return false;
			}
			catch (Exception ex)
			{
				error = ex.Ext_GetFullMessage();
				return null;
			}
			return true;
		}

		//return file ids
		public UpdateAppInfo GetUpdateInfo(string AppName, LocalSystemInfo localSystemInfo, out string error)
		{
			error = null;
			try
			{
				string AppDir = Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory + @"Updates").FirstOrDefault(x => Path.GetFileName(x).Split('-')[1] == AppName);
                //File.AppendAllText($@"{AppDir}\UpdateLog.txt", localSystemInfo.ComputerName + $" Starts Update,{DateTime.Now.ToString()}" + Environment.NewLine);
                //check requirement
                if (File.Exists($@"{AppDir}\systemRequirementInfo.json"))
                {
                    var req = JsonConvert.DeserializeObject<AppRequirementSystemInfo>(File.ReadAllText($@"{AppDir}\systemRequirementInfo.json"));
                    string er;
                    bool vald = req.Validate(localSystemInfo,out er);
                    if (!vald) { error = $"minimumRequirement|{er}"; return null; }
                }
                if (string.IsNullOrEmpty(AppDir)) return null;
                
				if (File.Exists($@"{AppDir}\init.txt"))
				{
                    return null;
				}
                var json = File.ReadAllText($@"{AppDir}\UpdateInfo.json");
                return JsonConvert.DeserializeObject<UpdateAppInfo>(json);
			}
			catch (Exception ex)
			{
				error = ex.Ext_GetFullMessage();
				return null;
			}
		}
        
	}
}
