using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using Updater.AppCode;
using System.Diagnostics;

namespace Updater
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{

		public static string TargetAppName { get; set; }
		public static bool UpdatePendingChanges { get; set; }
		/// <summary>
		/// no windows will be shown
		/// </summary>
		public static bool SilentUpdate { get; set; }
		//target app wants to Update at startup
		public static bool AutoUpdate { get; set; }
		//develop testing
		public static bool IsDebugging { get; set; } = true;

		public static UpdaterServiceReference.UpdateServiceClient UpdateServiceClient1 { get; set; }
		/// <summary>
		/// اولین نقطه ی اجرای نرم افزار
		/// </summary>
		/// <param name="e"></param>
		protected override void OnStartup(StartupEventArgs e)
		{
			UpdatePendingChanges = false;
			/*
            Copy This Code To You App
            string js = JsonConvert.SerializeObject(new
            {
            ServerUrl = "http://localhost:57718/UpdateService.svc",
            TargetAppName = "SampleApp"
            });
            js = js.Replace("\"", "'");
            Process.Start("Updater.exe", js);
            */
			//Shutdown process on exceptions
			try
			{
				//var tfdfdsgf = Cls_Helpers.GetInformation("Win32_PnPEntity");
				//this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
				string inputArg = "";
				if (IsDebugging)
				{
					inputArg = "{'ServerUrl':'http://localhost:57718/UpdateService.svc','TargetAppName':'SampleApp','SilentUpdate':'false'}";
				}
				else
				{
					if (e.Args.Length == 0 || string.IsNullOrEmpty(e.Args[0]))
					{
						MessageBox.Show("Invalid Call Of Updater App,Please Check Args");
						this.Shutdown();
						return;
					}
					inputArg = e.Args[0];
				}
				var JsonObject = new
				{
					ServerUrl = (string)null,
					TargetAppName = (string)null,
					SilentUpdate = (bool)false,
					AutoUpdate = (bool)false
				};

				JsonObject = JsonConvert.DeserializeAnonymousType(inputArg, JsonObject);
				TargetAppName = JsonObject.TargetAppName;
				SilentUpdate = JsonObject.SilentUpdate;
				AutoUpdate = JsonObject.AutoUpdate;

				if (string.IsNullOrEmpty(JsonObject?.TargetAppName) || string.IsNullOrEmpty(JsonObject?.ServerUrl))
				{
					MessageBox.Show("Invalid Call Of Updater App,Please Check Args");
					this.Shutdown();
					return;
				}

				UpdateServiceClient1 = new UpdaterServiceReference.UpdateServiceClient();
				UpdateServiceClient1.Endpoint.Address = new System.ServiceModel.EndpointAddress(JsonObject.ServerUrl);

				//var error = UpdateServiceClient1.InitialUpdateApp("1-SampleApp", UpdaterServiceReference.UpdateVersionPriority.Normal, "3276432%$#@%$9jbkvd");
				//MessageBox.Show(error);
				//this.Shutdown();
				//return;

				Win_Updater.Instance.ShowDialog();

			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
				this.Shutdown();
				return;
			}

		}

		public static void StartProcess(ClientParams clientParams)
		{
			try
			{
				UpdateServiceClient1 = new UpdaterServiceReference.UpdateServiceClient();
				UpdateServiceClient1.Endpoint.Address = new System.ServiceModel.EndpointAddress(clientParams.ServerUrl);
				
				Win_Updater.Instance.ShowDialog();
			}
			catch (Exception ex)
			{
				AppendLog(ex.Ext_Message(),LogTypes.Error);
			}
			
		}


		static int logCounter = 0;

		public static void AppendLog(string Msg, LogTypes logType = LogTypes.Info)
		{
			File.AppendAllText(AppDomain.CurrentDomain.BaseDirectory + $"UpdateLog.txt", $"{logCounter.ToString()}_{DateTime.Now.ToString()}_" + Msg + Environment.NewLine);
			if (!App.SilentUpdate)
				Win_Updater.Instance.Log($"{DateTime.Now.ToShortTimeString()}_{Msg}", logType);
			logCounter++;
		}
	}
}
