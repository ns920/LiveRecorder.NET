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
        public int Status { get; set; } = 0;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public string LivePassword { get; set; } = string.Empty;
        public Dictionary<string,string> CustomHeader { get; set; } = new Dictionary<string, string>();

        public bool MessageSend { get; set; } = false;
    }
}
