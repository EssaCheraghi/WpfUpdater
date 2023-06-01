using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using UpdaterService.Models;

namespace UpdaterService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IUpdateService" in both code and config file together.
    [ServiceContract]
    public interface IUpdateService
    {
        [OperationContract]
        bool? SelfUpdateNeeded(string JsonInput, out string error);
        [OperationContract]
        string GetFileBlock(string JsonInput, out string error);
        [OperationContract]
        UpdateAppInfo GetUpdateInfo(string AppName, LocalSystemInfo localSystemInfo, out string error);
        
    }
}
