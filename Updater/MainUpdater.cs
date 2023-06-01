using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Updater.UpdaterServiceReference;
using Updater.AppCode;
using Updater.Models;
using System.Threading.Tasks;

namespace Updater
{

    //notes
    //modification date file compare has a problem for first update of app it will download all files

    public class MainUpdater
    {
        DateTime StartTime = DateTime.Now;
        UpdateAppInfo updateAppInfo_LatestVersion { get; set; }
        BackgroundWorker backgroundWorker = new BackgroundWorker();
        UpdateAppInfo updateAppInfo_SavedVersion { get; set; }
        List<string> Skips { get; set; }
        UpdateServiceClient UpdateServiceClient1 { get; set; }
        bool inited = false;
        bool updateCanceled = false;
        long _UpdateSize;
        //bool RestoredBackup;
        public event Action<int> Evt_ProgressChanged;
        public event Action<UpdateEventState> Evt_UpdateEvents;
        public event Action<LogTypes, string> Evt_Logs;
        public long UpdateSize
        {
            get
            {
                if (!inited) throw new Exception("Call Init First");
                return _UpdateSize;
            }
        }
        public long RemainingUpdateSize
        {
            get
            {
                if (!inited) throw new Exception("Call Init First");
                return updateAppInfo_SavedVersion.Ext_GetRemainingSize(Skips);
            }
        }
        public UpdateVersionPriority UpdatePriority
        {
            get
            {
                if (!inited) throw new Exception("Call Init First");
                return updateAppInfo_LatestVersion.updatePriority;
            }
        }
        public ClientParams ClientParams { get; set; }
        public TimeSpan UpdateDuration { get { return DateTime.Now - StartTime; } }



        public MainUpdater(ClientParams clientParams)
        {
            try
            {
                Skips = new List<string>();
                ClientParams = clientParams;
                HttpBindingBase basicHttpBinding = new BasicHttpBinding();
                if (clientParams.ServerUrl.ToLower().StartsWith("https")) 
                {
                    basicHttpBinding = new BasicHttpsBinding();
                }
                basicHttpBinding.MaxReceivedMessageSize = 20000000;
                basicHttpBinding.ReaderQuotas.MaxDepth = 32;
                basicHttpBinding.ReaderQuotas.MaxArrayLength = 20000000;
                basicHttpBinding.ReaderQuotas.MaxStringContentLength = 20000000;
                basicHttpBinding.AllowCookies = true;
                basicHttpBinding.ReceiveTimeout = TimeSpan.FromMinutes(10);
                basicHttpBinding.CloseTimeout = TimeSpan.FromMinutes(10);
                basicHttpBinding.OpenTimeout = TimeSpan.FromMinutes(10);
                basicHttpBinding.SendTimeout = TimeSpan.FromMinutes(10);

                var endpoint = new System.ServiceModel.EndpointAddress(clientParams.ServerUrl);
                UpdateServiceClient1 = new UpdateServiceClient(basicHttpBinding, endpoint);
            }
            catch (Exception ex)
            {
                LogError(ex.Ext_Message());
            }
        }

        public void StartUpdate()
        {
            if (!inited) throw new Exception("Call Init First");
            backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.WorkerReportsProgress = true;
            updateCanceled = false;
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// call at startup
        /// </summary>
        /// <returns></returns>
        public bool UnapplyUpdateExists()
        {
            return Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output");
        }
        public string GetNewFeatures()
        {
            if (!inited) throw new Exception("Call Init First");
            return updateAppInfo_LatestVersion.NewFeatures;
        }

        public List<string> GetBackups()
        {
            return Directory.GetDirectories(AppDomain.CurrentDomain.BaseDirectory).Select(x => Path.GetFileName(x)).Where(x => x.StartsWith("_Backup_")).ToList();
        }
        public void RestoreBackup(string BackupName)
        {
            var bDir = AppDomain.CurrentDomain.BaseDirectory + BackupName;

            if (!Directory.Exists(bDir)) throw new Exception("Backup Not Found");

            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + $@"\_restoreBackup.bat",
                $@"
                taskkill /IM {ClientParams.TargetAppName}.exe /F
                xcopy ""{bDir}\*.*"" ""{AppDomain.CurrentDomain.BaseDirectory}"" /s /y
                start /d""{(AppDomain.CurrentDomain.BaseDirectory +"\" "+ ClientParams.TargetAppName + ".exe")}             
                ");
            Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"\_restoreBackup.bat");
        }
        public void RestartAppAndApplyUpdate()
        {
            try
            {
                var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output", "*.*", SearchOption.AllDirectories);
                if (files.Length == 0)
                    Evt_Logs.Invoke(LogTypes.Error, "No File Found In Output Directory to restore");


                var len = (AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output").Length;
                if (ClientParams.CreateBackup)
                {
                    //create backup from files that will be overwriten
                    var backdir = AppDomain.CurrentDomain.BaseDirectory + $"_Backup_{DateTime.Now.Ext_ToFilename()}";

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

                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\_copy.bat",
                    $@"
                taskkill /IM {ClientParams.TargetAppName}.exe /F
                xcopy ""{(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output\") }*.*"" ""{AppDomain.CurrentDomain.BaseDirectory}"" /s /y
                start /d""{(AppDomain.CurrentDomain.BaseDirectory + "\" "+ ClientParams.TargetAppName + ".exe")}
                del ""{(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output") }"" /s /f /q
                rd ""{(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\output") }"" /s  /q
                del ""{(AppDomain.CurrentDomain.BaseDirectory + @"_restoreBackup.bat") }"" /s /f /q
                ");
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\_copy.bat");
            }
            catch (Exception ex)
            {
                LogError("خطا در زمان جایگزینی فایل های دانلود شده" + Environment.NewLine + ex.Ext_Message());
            }
        }

        public void DeleteUpdateInfo()
        {
            if(Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "updatefiles"))
            Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "updatefiles", true);
        }
        /// <summary>
        /// returns true if inited successfully
        /// </summary>
        /// <returns></returns>
		public async Task<bool> InitAsync()
        {
            try
            {
                Skips = new List<string>();

                if (ClientParams.Force)
                {
                    DeleteUpdateInfo();
                }

                //RestoredBackup = File.Exists(AppDomain.CurrentDomain.BaseDirectory + "_restoreBackup.bat");

                //if (RestoredBackup)
                //{
                //    DeleteUpdateInfo();
                //}
            }
            catch(Exception ex)
            {
                LogError(ex.Ext_Message());
                return false;
            }

            var LocalSystemInfo = await Cls_Helpers.GetLocalSystemInfo();
            try
            {
                string error;
                updateAppInfo_LatestVersion = UpdateServiceClient1.GetUpdateInfo(ClientParams.TargetAppName, LocalSystemInfo, out error);

                if (error != null)
                {
                    if (error.StartsWith("minimumRequirement|"))
                    {
                        LogError("minimum Requirement " + error.Split('|')[1]);
                        return false;
                    }
                    Evt_Logs.Invoke(LogTypes.Info, error);
                    Evt_UpdateEvents.Invoke(UpdateEventState.ErrorServerSide);
                    return false;
                }
                if (updateAppInfo_LatestVersion == null)
                {
                    Evt_UpdateEvents.Invoke(UpdateEventState.NoUpdateFound);
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Ext_Message());
                Evt_UpdateEvents.Invoke(UpdateEventState.ErrorConnectingServer);
                return false;
            }

            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\parts"))
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\parts");
            
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\UpdateInfo.json"))
            {
                var json = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\UpdateInfo.json");
                updateAppInfo_SavedVersion = JsonConvert.DeserializeObject<UpdateAppInfo>(json);

                if (updateAppInfo_SavedVersion.Ext_HasChanged(updateAppInfo_LatestVersion))
                {
                    ClearExpiredParts();
                    updateAppInfo_SavedVersion = updateAppInfo_LatestVersion;
                    SaveRestorePoint();
                }
            }
            else
            {
                updateAppInfo_SavedVersion = updateAppInfo_LatestVersion;
                SaveRestorePoint();
            }
            try
            {
                InitParts();
            }
            catch(Exception ex)
            {
                LogError(ex.Ext_Message());
                return false;
            }
            if (ClientParams.Force)
                _UpdateSize = updateAppInfo_SavedVersion.Ext_GetTotalSize();
            else
                _UpdateSize = updateAppInfo_SavedVersion.Ext_GetRemainingSize(Skips);

            if (_UpdateSize == 0)
            {
                Evt_UpdateEvents.Invoke(UpdateEventState.AppIsUpdateNow);
                return false;
            }
            //App Is Up To Date , Concat Parts if available
            //var msgres = MessageBox.Show($@"New Version Available For This Software Size:{remain.Ext_EasyReadLength()} ,Prority:{updateAppInfo_SavedVersion.updatePriority} ,Do you want to update now?"
            //    + Environment.NewLine + "NewFeatures : " + updateAppInfo_SavedVersion.NewFeatures
            //    , "Update", MessageBoxButton.YesNoCancel);
            inited = true;
            Evt_UpdateEvents.Invoke(UpdateEventState.UpdateRequires);
            return true;
        }
        public bool IsNewVersionAvailable()
        {
            if (!inited) throw new Exception("Call Init First");
            return updateAppInfo_SavedVersion.Version != updateAppInfo_LatestVersion.Version;
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

                        var physicalFilechanged = f1.Ext_HasChangedWithPhysicalFile(updateAppInfo_SavedVersion.files[i]);

                        if (!physicalFilechanged && !ClientParams.Force)
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
                            if (lat == -1)
                            {
                                if (!ClientParams.Force)
                                    Skips.Add(updateAppInfo_SavedVersion.files[i].FileName);
                            }
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
                SaveRestorePoint();
                if (updateCanceled) return;
                Evt_Logs.Invoke(LogTypes.Info, "Merging update parts");
                //Thread.Sleep(1000);

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
                Evt_UpdateEvents.Invoke(UpdateEventState.UpdateCompleted);
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
            StartTime = DateTime.Now;

            try
            {
                var LastPh = updateAppInfo_SavedVersion.Ext_GetPhysicalPartCount();
                long LastPart = updateAppInfo_SavedVersion.Ext_GetLastPart();

                if (LastPh == 0) Evt_UpdateEvents.Invoke(UpdateEventState.UpdateStarted);
                else Evt_UpdateEvents.Invoke(UpdateEventState.UpdateResumed);

                foreach (var f in updateAppInfo_SavedVersion.files)
                {
                    if (Skips.Contains(f.FileName)) continue;
                    for (int i = f.StartPartId; i <= f.EndPartId; i++)
                    {
                        if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}")) continue;
                        Evt_Logs.Invoke(LogTypes.Info, $"Getting Part_{i} From Server");
                        Thread.Sleep(ClientParams.LatencyBetweenParts);
                        var ClientObject2 = new
                        {
                            AppId = (int?)updateAppInfo_SavedVersion.AppId,
                            BlockNumber = i
                        };
                        string error2 = null;
						
						
						var base64 = UpdateServiceClient1.GetFileBlock(JsonConvert.SerializeObject(ClientObject2), out error2);

						var bytes = System.Convert.FromBase64String(base64);
						if (bytes == null) throw new Exception($"App={ClientObject2.AppId}, Block={ClientObject2.BlockNumber} Not Exist In Server");
                        File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}", bytes);
                        //if (Math.Round(UpdateDuration.TotalSeconds) % 5 == 0)
                        //{
                        //	backgroundWorker.ReportProgress(Convert.ToInt32(((double)LastPh / LastPart) * 100), "Saving Restore Point...");
                        //}
                        if (backgroundWorker.CancellationPending)
                        {
                            updateCanceled = true;
                            Evt_UpdateEvents.Invoke(UpdateEventState.UpdateCanceled);
                            return;
                        }
                        var percent = Convert.ToInt32(((double)LastPh / LastPart) * 100);
                        backgroundWorker.ReportProgress(percent, $"Updating {percent}%");
                        if (LastPh < LastPart) LastPh++;
                    }
                }

                if (UpdateDuration.TotalMinutes >= 5)
                {
                    //check updateinfo.json again by restart updater.exe
                }
            }
            catch (Exception ex)
            {
                LogError(ex.Ext_Message());
            }
        }

        public void CancelAsync()
        {
            try
            {
                backgroundWorker?.CancelAsync();
            }
            catch { }
        }

        //calls every 10 sec
        void SaveRestorePoint()
        {
            Evt_Logs.Invoke(LogTypes.Info, "Saving Restore Point");
            string JsLog = JsonConvert.SerializeObject(updateAppInfo_SavedVersion);
            System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\UpdateInfo.json", JsLog);
            //appends bytes to files also here
        }
        void ClearExpiredParts()
        {
            try
            {
                foreach (var s in updateAppInfo_SavedVersion.files)
                {
                    var lastpartId = s.Ext_GetLatestPartId();
                    if (lastpartId == -1) continue;
                    if (updateAppInfo_LatestVersion.files.Any(x => (x.StartPartId <= lastpartId && x.EndPartId >= lastpartId) && x.Ext_HasChanged(s)))
                        s.Ext_ClearAllParts();
                }

                var AllParts = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + @"updatefiles\parts\").Select(x => Convert.ToInt32(System.IO.Path.GetFileNameWithoutExtension(x).Replace("part_", "")));
                var exp = AllParts.Where(x => updateAppInfo_LatestVersion.files.Any(y => y.StartPartId <= x && y.EndPartId >= x)).ToArray();
                for (int j = 0; j < exp.Length; j++)
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{exp[j]}");
            }
            catch (Exception ex)
            {
                Evt_Logs.Invoke(LogTypes.Error, "Error While deleting expired parts," + ex.Ext_Message());
            }
        }

        int logCounter = 0;
        public void LogError(string errorText)
        {
            if (ClientParams?.LogToTxtFile ?? false)
                File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + $"UpdateLog.txt", $"{logCounter.ToString()}_{DateTime.Now.ToString()}_" + errorText + Environment.NewLine);
            Evt_Logs.Invoke(LogTypes.Error, errorText);
            logCounter++;
        }

        public static string EasyReadLength(long FileLength)
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
    }
}
