using LiveRecorder.NET.Models.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Models
{
    public class Streamer
    {
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public StreamerStatus Status { get; set; } = 0;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string LivePassword { get; set; } = string.Empty;
        public string Quality { get; set; } = "high";
        public Dictionary<string, string> CustomHeader { get; set; } = new Dictionary<string, string>();
        /// <summary>
        /// 消息是否已发送
        /// </summary>
        public bool MessageSend { get; set; } = false;
        /// <summary>
        /// 是否发送消息
        /// </summary>
        public bool IsNotify { get; set; } = true;
        /// <summary>
        /// 是否录制
        /// </summary>
        public bool IsRecord { get; set; } = true;
    }
}
