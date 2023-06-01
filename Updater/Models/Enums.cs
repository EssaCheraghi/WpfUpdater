using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Updater.Models
{
    [Flags]
    public enum FileChangeBy
    {
        Length = 2,
        ModifiedDate = 4,
        AssemplyVersion = 8
    }



    public enum UpdateEventState
    {
        NoUpdateFound = 0,
        UpdateRequires,
        ErrorConnectingServer,
        AppIsUpdateNow,
        ErrorServerSide,
        UpdateCanceled,
        UpdateStarted,
        UpdateResumed,
        UpdateCompleted
    }


    public enum LogTypes
    {
        Success = 0,
        Info,
        Warning,
        Error
    }


}
