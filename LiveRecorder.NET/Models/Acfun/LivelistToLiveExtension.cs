using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Models.Acfun
{
    public static class LivelistToLiveExtension
    {
        public static AcfunLive ToAcfunLive(this Livelist livelist)
        {
            if (!int.TryParse(livelist.href, out int uid))
            {
                uid = 0;
            }
            return new AcfunLive()
            {
                liveId = livelist.liveId,
                name = livelist.user.name,
                startTime = livelist.createTime.Value,
                streamName = livelist.streamName,
                title = livelist.title,
                uid = uid
            };
        }
    }
}
