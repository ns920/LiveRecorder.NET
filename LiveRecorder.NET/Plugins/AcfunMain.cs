using AcfunApi;
using LiveRecorder.NET.Data;
using LiveRecorder.NET.IServices;
using LiveRecorder.NET.Models;
using LiveRecorder.NET.Models.Acfun;
using LiveRecorder.NET.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Plugins
{
    /// <summary>
    /// Acfun直播服务主插件，用于查询信息并保存到sqlite，不会直接录制视频。
    /// </summary>
    public class AcfunMain : IWebsiteService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AcfunlivedbDbContext _dbContext;
        private readonly AcfunApiRequest _acfunApiRequest;
        private readonly ILogger<AcfunMain> _logger;
        public List<AcfunLive> _nowLives;
        public AcfunMain(IHttpClientFactory httpClientFactory, AcfunlivedbDbContext dbContext, AcfunApiRequest acfunApiRequest,
            ILogger<AcfunMain> logger)
        {
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
            _acfunApiRequest = acfunApiRequest;
            _logger = logger;
        }
        /// <summary>
        /// 只需要实现这个方法返回不在直播
        /// </summary>
        /// <param name="streamer"></param>
        /// <returns></returns>
        public async Task<int> CheckLiveStatus(Streamer streamer)
        {
            string url = $"https://live.acfun.cn/api/channel/list?count=100000&pcursor=0";

            HttpClient client = _httpClientFactory.CreateClient();
            try
            {
                var recentLiveList = GetLiveDataRecent();
                var origindataFromApiString = await _acfunApiRequest.GetNoSign(url);
                var isError = JsonConvert.DeserializeObject<JToken>(origindataFromApiString)?["isError"];
                if (isError is not null && (bool)isError == true)
                {
                    _logger.LogError("ACFUN API获取列表失败");
                    return 0;
                }
                //实际获取失败会无法反序列化抛出异常
                var origindataFromApi = JsonConvert.DeserializeObject<OriginalLiveData>(origindataFromApiString);
                var liveListFromApi = origindataFromApi?.liveList.ToList().Select(x => x.ToAcfunLive()).ToList();
                var newLiveList = liveListFromApi?.Where(x => !recentLiveList.Select(x => x.liveId).ToList().Contains(x.liveId)).ToList() ?? new List<AcfunLive>();
                _nowLives = liveListFromApi ?? new List<AcfunLive>();
                _dbContext.Lives.AddRange(newLiveList);
                _dbContext.SaveChanges();
                _logger.LogInformation("ACFUN API获取列表成功");
            }
            catch (Exception ex)
            {

            }
            return 0;
        }
        private List<AcfunLive> GetLiveDataRecent()
        {
            //获取2天内的直播
            var timestamp = GetTimeStamp.GetMiliSecond(DateTime.Now.AddDays(-2));
            return _dbContext.Lives.Where(x => x.startTime > timestamp).ToList();
        }


        public async Task<bool> StartRecording(Streamer streamer)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> EndRecording(Streamer streamer)
        {
            return true;
        }
    }
}
