using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Models
{
    /// <summary>
    /// 录制信息类
    /// </summary>
    public class RecordingInfo
    {
        public string Name { get; set; } = string.Empty; // 主播名称
        public string Url { get; set; }
        public DateTime StartTime { get; set; }
    }
}
