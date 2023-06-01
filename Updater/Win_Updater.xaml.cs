using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using Updater.UpdaterServiceReference;
using Updater.AppCode;
using System.ComponentModel;
using System.Threading;
using System.IO.Compression;
using System.Diagnostics;

namespace Updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Win_Updater : Window
    {
		public static Win_Updater Instance { get; set; } = new Win_Updater();
		
		DateTime StartTime = DateTime.Now;
		TimeSpan UpdateDuration { get { return DateTime.Now - StartTime; } }
		UpdateAppInfo updateAppInfo_LatestVersion { get; set; }
		public BackgroundWorker backgroundWorker = new BackgroundWorker();
		public event Action<int, string> Evt_ProgressChanged;
		public UpdateAppInfo updateAppInfo_SavedVersion { get; set; }
		public static List<string> Skips = new List<string>();

		public Win_Updater()
        {
            InitializeComponent();
			Txb_Description.Text = "";
			Prg_DownloadProcess.Value = 0;
		}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
			this.Hide();
			var state = Init();
			if (!App.SilentUpdate)
			{
				this.Show();
			}
			if (state == InitUpdateState.AppIsUpdateNow) 
			{ 
				App.AppendLog("App is update To Latest version now.",LogTypes.Success);
				//if (App.SilentUpdate) { App.Current.Shutdown();  }
				//return;
			}
			if (state == InitUpdateState.ErrorConnectingServer)
			{
				App.AppendLog($"Can not connect to server {App.UpdateServiceClient1.Endpoint.Address.Uri.AbsoluteUri}", LogTypes.Success);
				if (App.SilentUpdate) { App.Current.Shutdown(); }
				return; 
			}
				if (state == InitUpdateState.ErrorServerSide)
			{
				App.AppendLog("server side error", LogTypes.Error);
				if (App.SilentUpdate) { App.Current.Shutdown();  }
				return;
			}

			
			Txb_Priority.Text = $"Update Priority :{updateAppInfo_SavedVersion.updatePriority}";
			if ((int)updateAppInfo_SavedVersion.updatePriority >= 2) // high or very high
				Btn_Cancel.IsEnabled = false;
			Evt_ProgressChanged += Main_Evt_ProgressChanged ;

			if(!App.SilentUpdate)
			this.Show();
			StartOrResume();
        }

		private void Main_Evt_ProgressChanged(int arg1, string arg2)
		{
			this.Dispatcher.Invoke(() => 
			{ 
				if(arg1 >= 0)
				Prg_DownloadProcess.Value = arg1;
				
			});

		}

		private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
		{
			App.Current.Shutdown();
		}

		public void Log(string Msg, LogTypes logType)
		{
			//logType must changes the color of message
			Rtxt_UpdateNotes.AppendText(Msg + Environment.NewLine);
			Rtxt_UpdateNotes.ScrollToEnd();
		}



		public InitUpdateState Init()
		{
			//must include some hardware information like ram size,Display Demistion,Processor info
			//and software info like os type

			try
			{
				var ClientObject = Cls_Helpers.GetLocalSystemInfo();
				string error;
				updateAppInfo_LatestVersion = App.UpdateServiceClient1.GetUpdateInfo(App.TargetAppName,ClientObject, out error);

				if (error != null)
				{
					return InitUpdateState.ErrorServerSide;
				}
			}
			catch
			{
				return InitUpdateState.ErrorConnectingServer;
			}
			if (App.IsDebugging)
			{
				if(!Process.GetProcesses().Any(x=>x.ProcessName == App.TargetAppName))
				Process.Start(App.TargetAppName);
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
			if (!App.SilentUpdate)
			{
				var msgres = MessageBox.Show($@"New Version Available For This Software Size:{remain.Ext_EasyReadLength()} ,Prority:{updateAppInfo_SavedVersion.updatePriority} ,Do you want to update now?"
				+ Environment.NewLine + "NewFeatures : " + updateAppInfo_SavedVersion.NewFeatures
				, "Update", MessageBoxButton.YesNoCancel); ;
				if (msgres != MessageBoxResult.Yes) { App.Current.Shutdown(); return InitUpdateState.UpdateAbort; }
			}
			StartTime = DateTime.Now;

			return InitUpdateState.UpdateRequires;
		}
		public bool IsNewVersionAvailable()
		{
			return updateAppInfo_SavedVersion.Version != updateAppInfo_LatestVersion.Version;
		}
		public void InitParts()
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
							var lat = f1.Ext_GetLatestPartId();
							if (lat == -1) Skips.Add(updateAppInfo_SavedVersion.files[i].FileName);
							else
								updateAppInfo_SavedVersion.files[i].StartPartId = lat;
						}
					}
				}
			}
		}
		public void StartOrResume()
		{
			backgroundWorker = new BackgroundWorker();
			backgroundWorker.WorkerSupportsCancellation = true;
			backgroundWorker.WorkerReportsProgress = true;
			backgroundWorker.DoWork += BackgroundWorker_DoWork;
			backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
			backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
			backgroundWorker.RunWorkerAsync();
		}

		private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			try
			{
				Evt_ProgressChanged.Invoke(100, "Saving Restore Point");
				SaveRestorePoint();
				//Create Backup folder for local files
				//.....
				// Concat Parts
				Evt_ProgressChanged.Invoke(100, "Concating update parts");
				Thread.Sleep(1000);

				try
				{
					if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output"))
						Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output", true);
					Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output");
				}
				catch (Exception ex)
				{
					throw new Exception("خطا در زمان حذف فایل های بروز رسانی قبلی" + Environment.NewLine + ex.Message);
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
				Evt_ProgressChanged.Invoke(100, "Update Complete");


				//ask user to restart target app
				var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output", "*.*", SearchOption.AllDirectories);
				//end process of target app
				var pr = Process.GetProcessesByName(App.TargetAppName);
				if (files.Length > 0 && pr.Length > 0)
				{
					if (!App.UpdatePendingChanges) { App.Current.Shutdown(); return; }
					if (MessageBox.Show($"Application {App.TargetAppName} requires restart", "restart", MessageBoxButton.YesNo) != MessageBoxResult.Yes) { App.Current.Shutdown(); return; }
					pr[0].Kill();
				}
				else
				{
					string message = $"Application {App.TargetAppName} Not Found To Kill,Updater will shutdown now.";
					if (files.Length == 0)
						message = "No Part Found to restore";
						if (App.IsDebugging) { MessageBox.Show(message); App.Current.Shutdown(); return; }
					else { App.AppendLog(message); App.Current.Shutdown(); return; }
				}
				//create backup from files that will be overwriten
				var len = (AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output").Length;
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
				//copy files to root
				foreach (var f in files)
				{
					if (!Directory.Exists(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory + f.Substring(len))))
						Directory.CreateDirectory(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory + f.Substring(len)));

					if (File.Exists(f))
						File.Copy(f, AppDomain.CurrentDomain.BaseDirectory + f.Substring(len), true);
				}

				if (files.Any(x => Path.GetFileNameWithoutExtension(x).ToLower() == "updater"))
				{
					//create batch file for replace updater.exe
					//in batch -> kill updater,replace it
					File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\output\copy.bat",
					$@"taskkill updater.exe /f
				       copy updatefiles\updater.exe *
					   start {App.TargetAppName}"
					);
					Process.Start("copy.bat");
				}
				else
					Process.Start(AppDomain.CurrentDomain.BaseDirectory + App.TargetAppName);
			}
			catch (Exception ex)
			{
				Evt_ProgressChanged.Invoke(100, "خطا در زمان استخراج پارت های دانلود شده" + Environment.NewLine + ex.Message);
			}
			App.Current.Shutdown();
		}

		private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			Evt_ProgressChanged.Invoke(e.ProgressPercentage, e.UserState == null ? "" : e.UserState.ToString());
		}

		private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
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
				if (LastPh != 0) Evt_ProgressChanged.Invoke(-1, "Getting Last Downloaded Parts Completed");

				foreach (var f in updateAppInfo_SavedVersion.files)
				{
					if (Skips.Contains(f.FileName)) continue;
					for (int i = f.StartPartId; i <= f.EndPartId; i++)
					{
						Evt_ProgressChanged.Invoke(-1, $"Getting Part_{i} From Server");
						Thread.Sleep(new Random().Next(300, 1000));
						var ClientObject2 = new
						{
							AppId = (int?)updateAppInfo_SavedVersion.AppId,
							BlockNumber = i
						};
						string error2 = null;
						var bytes = App.UpdateServiceClient1.GetFileBlock(JsonConvert.SerializeObject(ClientObject2), out error2);
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
			catch(Exception ex)
			{
				if (App.IsDebugging) MessageBox.Show(ex.Message);
				else App.AppendLog(ex.Message);
			}
		}

		public void CancelAsync()
		{
			backgroundWorker.CancelAsync();
		}

		//calls every 10 sec
		public void SaveRestorePoint()
		{
			if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles"))
				Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles");

			if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\parts"))
				Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\parts");

			string JsLog = updateAppInfo_SavedVersion.Ext_XmlSerialize();
			System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\UpdateInfo.xml", JsLog);
			//appends bytes to files also here
		}
		public void ClearExpiredParts()
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
				MessageBox.Show(ex.Message, "Error While deleting expired parts");
			}
		}

	}
}
