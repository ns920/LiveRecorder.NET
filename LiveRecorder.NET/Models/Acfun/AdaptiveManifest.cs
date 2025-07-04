using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Models.Acfun
{
    public class AdaptiveManifest

    {
        public string version { get; set; }
        public int businessType { get; set; }
        public int mediaType { get; set; }
        public bool hideAuto { get; set; }
        public bool manualDefaultSelect { get; set; }
        public int stereoType { get; set; }
        public Adaptationset[] adaptationSet { get; set; }

        public class Adaptationset
        {
            public int id { get; set; }
            public int duration { get; set; }
            public Representation[] representation { get; set; }
        }

        public class Representation
        {
            public int id { get; set; }
            public string url { get; set; }
            public string[] backupUrl { get; set; }
            public string m3u8Slice { get; set; }
            public int maxBitrate { get; set; }
            public int avgBitrate { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public string qualityType { get; set; }
            public string qualityLabel { get; set; }
            public bool featureP2sp { get; set; }
            public bool hidden { get; set; }
            public bool disableAdaptive { get; set; }
            public bool defaultSelect { get; set; }
        }
    }
}
