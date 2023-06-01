using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Updater.UpdaterServiceReference;

namespace Updater.Models
{
	public static class ClientAppInfo
	{
		public static long Ext_GetLastPart(this UpdateAppInfo updateAppInfo)
		{
			//.Where(x=>!Win_Updater.Skips.Contains(x.FileName))
			return updateAppInfo.files.Max(x => x.EndPartId);
		}
		public static long Ext_GetPhysicalPartCount(this UpdateAppInfo updateAppInfo)
		{
			if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts"))
				return Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts").Length;
			return 0;
		}
		public static long Ext_GetRemainingSize(this UpdateAppInfo updateAppInfo,List<string> Skips)
		{
			long remain = 0;
			foreach (var f in updateAppInfo.files)
			if(!Skips.Contains(f.FileName))
				remain += f.Ext_RemainingBytes();
			return remain;
		}
        public static long Ext_GetTotalSize(this UpdateAppInfo updateAppInfo)
        {
            long total = 0;
            foreach (var f in updateAppInfo.files)
                    total += f.Length;
            return total;
        }
        public static bool Ext_HasChanged(this UpdateAppInfo updateAppInfo, UpdateAppInfo updateAppInfo2, FileChangeBy fileChangeBy = FileChangeBy.Length)
		{
			if (updateAppInfo.AppId != updateAppInfo2.AppId) return true;
			if (updateAppInfo.Version != updateAppInfo2.Version) return true;
			if (updateAppInfo.files.Length != updateAppInfo2.files.Length) return true;

			foreach (var f in updateAppInfo.files)
				if (!updateAppInfo2.files.Select(x=>x.FileName).Contains(f.FileName)) return true;
				else if (updateAppInfo2.files.Any(x => x.FileName == f.FileName && (x.Length != f.Length && fileChangeBy.HasFlag(FileChangeBy.Length)) 
                || (x.ModifiedTime != f.ModifiedTime && fileChangeBy.HasFlag(FileChangeBy.ModifiedDate))
                ))
					return true;
				
			return false;
		}
	}
}
