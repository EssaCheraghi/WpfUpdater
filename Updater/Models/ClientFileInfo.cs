using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Updater.UpdaterServiceReference;

namespace Updater.Models
{
    public static class FileInfoHelper
    {
        public static UpdateFileInfo GetLocalVersion(this UpdateFileInfo updateFileInfo)
		{
			UpdateFileInfo ret = new UpdateFileInfo();
			ret.FileName = AppDomain.CurrentDomain.BaseDirectory + updateFileInfo.FileName;
			if (!File.Exists(ret.FileName)) return null;
			var fileinfo = new FileInfo(ret.FileName);
			ret.Length = fileinfo.Length;
			ret.CreationTime = fileinfo.CreationTime;
			ret.ModifiedTime = fileinfo.LastWriteTime;
			ret.IsZipped = Path.GetExtension(ret.FileName).ToLower() == ".rar";
			return ret;
		}
		public static string GetVersion(this UpdateFileInfo updateFileInfo)
		{
			return FileVersionInfo.GetVersionInfo(updateFileInfo.FileName).FileVersion;
		}

		public static void Ext_ClearAllParts(this UpdateFileInfo updateFileInfo)
        {
            for (int i = updateFileInfo.StartPartId; i < updateFileInfo.EndPartId; i++)
            {
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}"))
                    File.Delete(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}");
            }
        }
		public static bool Ext_HasNotExistPhysicalFile(this UpdateFileInfo server)
		{
			return server.GetLocalVersion() == null;
		}
		public static bool Ext_HasChangedWithPhysicalFile(this UpdateFileInfo server, UpdateFileInfo JsonSavedFileInfo, FileChangeBy fileChangeBy= FileChangeBy.Length | FileChangeBy.ModifiedDate)
        {
			bool ret = false;
			var local = server.GetLocalVersion();
			if (local == null) return true;
			if (fileChangeBy.HasFlag(FileChangeBy.Length) && local.Length != server.Length) ret = true;
            if (fileChangeBy.HasFlag(FileChangeBy.ModifiedDate) && JsonSavedFileInfo.ModifiedTime != server.ModifiedTime) ret = true;

            return ret;
        }
		public static bool Ext_HasChanged(this UpdateFileInfo updateFileInfo, UpdateFileInfo updateFileInfo2, FileChangeBy fileChangeBy = FileChangeBy.Length | FileChangeBy.ModifiedDate)
		{
			if (fileChangeBy.HasFlag(FileChangeBy.Length) && updateFileInfo.Length != updateFileInfo2.Length)
				return true;
            if (fileChangeBy.HasFlag(FileChangeBy.ModifiedDate) && updateFileInfo.ModifiedTime != updateFileInfo2.ModifiedTime)
                return true;

            return false;
			
		}
		public static bool Ext_FirstPartExists(this UpdateFileInfo updateFileInfo)
		{
			return File.Exists(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{updateFileInfo.StartPartId}");
		}
		public static int Ext_GetLatestPartId(this UpdateFileInfo updateFileInfo)
        {
			for (int i = updateFileInfo.StartPartId; i <= updateFileInfo.EndPartId; i++)
            {
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}"))
                    return i;
            }
            return -1;
        }
        public static int Ext_DownloadedPartCount(this UpdateFileInfo updateFileInfo)
        {
			var f = Ext_GetLatestPartId(updateFileInfo);
			if (f == -1) f = updateFileInfo.EndPartId;
			return f - updateFileInfo.StartPartId;
        }
        public static bool Ext_DownloadedPartsComplete(this UpdateFileInfo updateFileInfo)
        {
            return Ext_DownloadedPartCount(updateFileInfo) == updateFileInfo.EndPartId;
        }
        public static int Ext_DownloadedBytes(this UpdateFileInfo updateFileInfo)
        {
            return Ext_DownloadedPartCount(updateFileInfo) * 100000;
        }
        public static int Ext_RemainingBytes(this UpdateFileInfo updateFileInfo)
        {
			int downloaded = 0;
            int totalToDownload = 0;// (updateFileInfo.EndPartId - updateFileInfo.StartPartId + 1) * 100000;
			for (int i = updateFileInfo.StartPartId; i <= updateFileInfo.EndPartId; i++)
			{
                if (i == updateFileInfo.EndPartId) totalToDownload += (int)(updateFileInfo.Length % 100000);
                else
                totalToDownload += 100000;

                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + $@"updatefiles\parts\part_{i}"))
                {
                    if (i == updateFileInfo.EndPartId) downloaded += (int)(updateFileInfo.Length % 100000);
                    else
                    downloaded += 100000;
                }
			}
			return (totalToDownload - downloaded);
        }

    }

}
