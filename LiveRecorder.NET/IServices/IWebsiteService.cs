using LiveRecorder.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.IServices
{
    interface IWebsiteService
    {
        public Task<int> CheckLiveStatus(Streamer streamer);
        /// <summary>
        /// 开始直播时调用
        /// </summary>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public Task<bool> StartRecording(Streamer streamer);
        /// <summary>
        /// 结束直播时调用
        /// </summary>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public Task<bool> EndRecording(Streamer streamer);
    }
}
