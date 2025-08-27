using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Models.Enum
{
    public enum StreamerStatus
    {
        Offline = 0,
        /// <summary>
        /// 只是在直播但是没在录制
        /// </summary>
        Living = 3,
        Recording = 1,
        Checking = 2,
    }
}
