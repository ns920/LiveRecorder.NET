using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Models.Acfun
{
    public class GetPlaybackResponse
    {
        public int result { get; set; }
        public GetPlaybackResponseData data { get; set; }
        public string host { get; set; }
    }

    public class GetPlaybackResponseData
    {
        public string adaptiveManifest { get; set; }
    }
}
