using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Updater.UpdaterServiceReference;

namespace Updater.AppCode
{
	public class MainUpdater
	{

		public event Action<int> Evt_ProgressChanged;
		public event Action<LogTypes, string> Evt_Logs;

		DateTime StartTime = DateTime.Now;
		TimeSpan UpdateDuration { get { return DateTime.Now - StartTime; } }
		UpdateAppInfo updateAppInfo_LatestVersion { get; set; }
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		UpdateAppInfo updateAppInfo_SavedVersion { get; set; }
		List<string> Skips = new List<string>();
		ClientParams _clientParams { get; set; }
		UpdateServiceClient UpdateServiceClient1 { get; set; }


		public MainUpdater(ClientParams clientParams)
		{
			try
			{
				_clientParams = clientParams;
				BasicHttpBinding basicHttpBinding = new BasicHttpBinding();
				basicHttpBinding.MaxReceivedMessageSize = 20000000;
				basicHttpBinding.ReaderQuotas.MaxDepth = 32;
				basicHttpBinding.ReaderQuotas.MaxArrayLength = 20000000;
				basicHttpBinding.ReaderQuotas.MaxStringContentLength = 20000000;
				basicHttpBinding.AllowCookies = true;
				var endpoint = new System.ServiceModel.EndpointAddress(clientParams.ServerUrl);
				UpdateServiceClient1 = new UpdateServiceClient(basicHttpBinding,endpoint);
			}
			catch (Exception ex)
			{
				LogError(ex.Ext_Message());
			}
		}
			   
		public void StartUpdate(bool force = false)//must get update size
		{
			var state = Init();

			if (state == InitUpdateState.AppIsUpdateNow)
			{
				Evt_Logs.Invoke(LogTypes.Success,"App is update To Latest version now.");
				//if (App.SilentUpdate) { App.Current.Shutdown();  }
				//return;
			}
			if (state == InitUpdateState.ErrorConnectingServer)
			{
				Evt_Logs.Invoke(LogTypes.Success, $"Can not connect to server {UpdateServiceClient1.Endpoint.Address.Uri.AbsoluteUri}");
				if (App.SilentUpdate) { App.Current.Shutdown(); }
				return;
			}
			if (state == InitUpdateState.ErrorServerSide)
			{
				Evt_Logs.Invoke(LogTypes.Error,"server side error");
				if (App.SilentUpdate) { App.Current.Shutdown(); }
				return;
			}

			backgroundWorker = new BackgroundWorker();
			backgroundWorker.WorkerSupportsCancellation = true;
			backgroundWorker.WorkerReportsProgress = true;
			backgroundWorker.DoWork += BackgroundWorker_DoWork;
			backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
			backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
			backgroundWorker.RunWorkerAsync();
		}

		public void RestartAppAndUpdate()
		{
			try
			{
			var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output", "*.*", SearchOption.AllDirectories);
			if (files.Length == 0)
				Evt_Logs.Invoke(LogTypes.Error, "No File Found In Output Directory to restore");


			var len = (AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output").Length;
			if (_clientParams.CreateBackup)
			{
				//create backup from files that will be overwriten
				var backdir = AppDomain.CurrentDomain.BaseDirectory + $"Backup_{DateTime.Now.Ext_ToFilename()}";

				foreach (var f in files)
				{
					if (!Directory.Exists(Path.GetDirectoryName(backdir + f.Substring(len))))
						Directory.CreateDirectory(Path.GetDirectoryName(backdir + f.Substring(len)));
					else if (!Directory.Exists(backdir))
						Directory.CreateDirectory(backdir);

					if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + f.Substring(len)))
						File.Copy(AppDomain.CurrentDomain.BaseDirectory + f.Substring(len), backdir + f.Substring(len), true);
				}
			}

			//copy files to root
			foreach (var f in files)
			{
				if (!Directory.Exists(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory + f.Substring(len))))
					Directory.CreateDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory + f.Substring(len)));

				//updateAppInfo_LatestVersion.files.First(x => x.FileName == ).Overwrite
				if (File.Exists(f))
					File.Copy(f, AppDomain.CurrentDomain.BaseDirectory + f.Substring(len), true);
			}

			if (files.Any(x => Path.GetFileNameWithoutExtension(x).ToLower() == "updater"))
			{
				File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\output\copy.bat",$@"start {_clientParams.TargetAppName}");
				Process.Start("copy.bat");
			}

			App.Current.Shutdown();
			}
			catch (Exception ex)
			{
				LogError("خطا در زمان جایگزینی فایل های دانلود شده" + Environment.NewLine + ex.Ext_Message());
			}
		}

		//public bool CheckMinimumRequirement(){

		//}
		InitUpdateState Init()
		{
			//must include some hardware information like ram size,Display Demistion,Processor info
			//and software info like os type

			try
			{
				var ClientObject = Cls_Helpers.GetLocalSystemInfo();
				string error;
				updateAppInfo_LatestVersion = UpdateServiceClient1.GetUpdateInfo(_clientParams.TargetAppName,ClientObject, out error);

				if (error != null)
				{
					Evt_Logs.Invoke(LogTypes.Info, error);
					return InitUpdateState.ErrorServerSide;
				}
			}
			catch (Exception ex)
			{
				LogError(ex.Ext_Message());
				return InitUpdateState.ErrorConnectingServer;
			}
			for (int u = 0; u < updateAppInfo_LatestVersion.files.Length; u++)
			{
				updateAppInfo_LatestVersion.files[u].FileName = updateAppInfo_LatestVersion.files[u].FileName.Replace("\\files", "");
			}
			if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\UpdateInfo.xml"))
			{
				updateAppInfo_SavedVersion = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\UpdateInfo.xml").Ext_XmlDeserialize<UpdateAppInfo>();

				ClearExpiredParts();
				if (updateAppInfo_SavedVersion.Ext_HasChanged(updateAppInfo_LatestVersion))
				{
					updateAppInfo_SavedVersion = updateAppInfo_LatestVersion;
					SaveRestorePoint();
				}
			}
			else
			{
				updateAppInfo_SavedVersion = updateAppInfo_LatestVersion;
				SaveRestorePoint();
			}
			InitParts();
			var remain = updateAppInfo_SavedVersion.Ext_GetRemainingSize();
			if (remain == 0) { return InitUpdateState.AppIsUpdateNow; }//App Is Up To Date , Concat Parts if available

			StartTime = DateTime.Now;

			return InitUpdateState.UpdateRequires;
		}
		void InitParts()
		{
			List<string> fchecked = new List<string>();

			foreach (var f1 in updateAppInfo_LatestVersion.files)
			{
				for (int i = 0; i < updateAppInfo_SavedVersion.files.Length; i++)
				{
					if (!fchecked.Contains(f1.FileName) && f1.FileName == updateAppInfo_SavedVersion.files[i].FileName)
					{
						fchecked.Add(f1.FileName);
						//var pExists = f1.Ext_HasNotExistPhysicalFile();
						//if(!pExists && f1)

						var physicalFilechanged = f1.Ext_HasChangedWithPhysicalFile();

						if (!physicalFilechanged)
							Skips.Add(updateAppInfo_SavedVersion.files[i].FileName);
						else
						if (f1.Ext_HasChanged(updateAppInfo_SavedVersion.files[i]))
						{
							f1.Ext_ClearAllParts();
							updateAppInfo_SavedVersion.files[i].StartPartId = f1.StartPartId;
						}
						else
						{
							int lat = -1;
							if (f1.Ext_HasNotExistPhysicalFile()) lat = f1.StartPartId;
							else lat = f1.Ext_GetLatestPartId();
							if (lat == -1) Skips.Add(updateAppInfo_SavedVersion.files[i].FileName);
							else
								updateAppInfo_SavedVersion.files[i].StartPartId = lat;
						}
					}
				}
			}
		}
		void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				Evt_Logs.Invoke(LogTypes.Info, "Saving Restore Point");
				SaveRestorePoint();
				//Create Backup folder for local files
				//.....
				// Concat Parts
				Evt_Logs.Invoke(LogTypes.Info, "Concating update parts");
				Thread.Sleep(1000);

				try
				{
					if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output"))
						Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output", true);
					Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output");
				}
				catch (Exception ex)
				{
					LogError("خطا در زمان حذف فایل های بروز رسانی قبلی" + "," + ex.Ext_Message());
				}

				foreach (var f in updateAppInfo_LatestVersion.files)
				{
					//if (Skips.Contains(f.FileName)) continue;
					if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{f.StartPartId}")) continue;
					List<byte> fb = new List<byte>();
					for (int i = f.StartPartId; i <= f.EndPartId; i++)
						fb.AddRange(File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}"));
					var dir = System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output" + f.FileName);
					if (!Directory.Exists(dir))
						Directory.CreateDirectory(dir);
					File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output" + f.FileName, fb.ToArray());

					//if (f.IsZipped)
					//{ ZipFile.ExtractToDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output" + f.FileName, System.IO.Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output" + f.FileName)); }//extract
				}
				Evt_ProgressChanged.Invoke(100);
				Evt_Logs.Invoke(LogTypes.Success, "Update Complete");
			}
			catch (Exception ex)
			{
				LogError("خطا در زمان استخراج پارت های دانلود شده" + Environment.NewLine + ex.Ext_Message());
			}
		}

		void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Evt_ProgressChanged.Invoke(e.ProgressPercentage);
		}

		void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				var LastPh = updateAppInfo_SavedVersion.Ext_GetPhysicalPartCount();
				long LastPart = updateAppInfo_SavedVersion.Ext_GetLastPart();

				foreach (var f in updateAppInfo_SavedVersion.files)
				{
					if (Skips.Contains(f.FileName)) continue;
					LastPart += f.EndPartId - f.StartPartId;
				}
				if (LastPh != 0)
				{
					Evt_ProgressChanged.Invoke(-1);
					Evt_Logs.Invoke(LogTypes.Info, "Getting Last Downloaded Parts Completed");
				}

				foreach (var f in updateAppInfo_SavedVersion.files)
				{
					if (Skips.Contains(f.FileName)) continue;
					for (int i = f.StartPartId; i <= f.EndPartId; i++)
					{
						Evt_ProgressChanged.Invoke(-1);
						Evt_Logs.Invoke(LogTypes.Info, $"Getting Part_{i} From Server");
						Thread.Sleep(new Random().Next(50, 300));
						var ClientObject2 = new
						{
							AppId = (int?)updateAppInfo_SavedVersion.AppId,
							BlockNumber = i
						};
						string error2 = null;
						var bytes = UpdateServiceClient1.GetFileBlock(JsonConvert.SerializeObject(ClientObject2), out error2);
						if (bytes == null) throw new Exception($"App={ClientObject2.AppId}, Block={ClientObject2.BlockNumber} Not Exist In Server");
						File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}", bytes);
						App.UpdatePendingChanges = true;
						if (UpdateDuration.TotalSeconds % 5 == 0)
						{
							backgroundWorker.ReportProgress(Convert.ToInt32(((double)LastPh / LastPart) * 100), "Saving Restore Point...");
							SaveRestorePoint();
						}
						if (backgroundWorker.CancellationPending) return;
						var percent = Convert.ToInt32(((double)LastPh / LastPart) * 100);
						backgroundWorker.ReportProgress(percent, $"Updating {percent}%");
						if (LastPh < LastPart) LastPh++;
					}
				}

				if (UpdateDuration.TotalMinutes >= 5)
				{
					//check updateinfo.xml again by restart updater.exe
				}
			}
			catch (Exception ex)
			{
				LogError(ex.Ext_Message());
			}
		}

		public void CancelAsync()
		{
			backgroundWorker.CancelAsync();
		}

		//calls every 10 sec
		void SaveRestorePoint()
		{
			if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles"))
				Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles");

			if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\parts"))
				Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\parts");

			string JsLog = updateAppInfo_SavedVersion.Ext_XmlSerialize();
			System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\UpdateInfo.xml", JsLog);
			//appends bytes to files also here
		}
		void ClearExpiredParts()
		{
			try
			{
				foreach (var s in updateAppInfo_SavedVersion.files)
				{
					if (!s.Ext_FirstPartExists()) continue;
					var lastpartId = s.Ext_GetLatestPartId();
					if (lastpartId == -1) lastpartId = s.EndPartId;
					var f = updateAppInfo_LatestVersion.files.FirstOrDefault(x => x.StartPartId <= lastpartId && x.EndPartId >= lastpartId && x.Ext_HasChanged(s));
					f?.Ext_ClearAllParts();
				}
			}
			catch (Exception ex)
			{
				Evt_Logs.Invoke(LogTypes.Error, "Error While deleting expired parts," + ex.Ext_Message());
			}
		}

		static int logCounter = 0;
		public void LogError(string errorText)
		{
			if (_clientParams?.LogToTxtFile ?? false)
				File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + $"UpdateLog.txt", $"{logCounter.ToString()}_{DateTime.Now.ToString()}_" + errorText + Environment.NewLine);
			Evt_Logs.Invoke(LogTypes.Error, errorText);
			logCounter++;
		}

	}
}
