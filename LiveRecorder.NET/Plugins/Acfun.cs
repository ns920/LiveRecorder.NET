using AcfunApi;
using LiveRecorder.NET.Data;
using LiveRecorder.NET.IServices;
using LiveRecorder.NET.Models;
using LiveRecorder.NET.Models.Acfun;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Plugins
{
    /// <summary>
    /// 对新结束的直播获取直播信息并保存到数据库，
    /// </summary>
    public class Acfun : IWebsiteService
    {
        private readonly AcfunMain _acfunMainService;
        private readonly AcfunApiRequest _acfunApiRequest;
        private readonly ILogger<Acfun> _logger;
        private readonly AcfunlivedbDbContext _dbContext;

        public Acfun(AcfunMain acfunMainService, AcfunApiRequest acfunApiRequest, ILogger<Acfun> logger,
            AcfunlivedbDbContext dbContext)
        {
            _acfunMainService = acfunMainService;
            _acfunApiRequest = acfunApiRequest;
            _logger = logger;
            _dbContext = dbContext;
        }
        /// <summary>
        /// 检查是否在直播
        /// </summary>
        /// <param name="streamer"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<int> CheckLiveStatus(Streamer streamer)
        {
            if(_acfunMainService!=null&&_acfunMainService._nowLives!=null)
            {
                var isLive = _acfunMainService._nowLives.Where(x => x.uid.ToString() == streamer.Channel).FirstOrDefault();
                if (isLive != null)
                {
                    return 1;
                }
                return 0;
            }
            else
            {
                _logger.LogWarning($"Acfun主模块尚未启动");
                return 0;
            }

        }

        /// <summary>
        /// 保存直播url到数据库
        /// </summary>
        /// <param name="streamer"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<bool> StartRecording(Streamer streamer)
        {
            return true;
        }
        public async Task<bool> EndRecording(Streamer streamer)
        {
            try
            {
                // 从数据库获取最新的直播记录，根据streamer的channel获取uid
                int uid = int.Parse(streamer.Channel);
                var latestLive = await _dbContext.Lives
                    .Where(x => x.uid == uid)
                    .OrderByDescending(x => x.startTime)
                    .FirstOrDefaultAsync();

                if (latestLive == null)
                {
                    _logger.LogError($"Acfun未找到主播 {uid} 的直播记录");
                    return false;
                }

                string liveId = latestLive.liveId;

                var form = new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>("liveId", liveId) };
                var requesturl = "https://api.kuaishouzt.com/rest/zt/live/playBack/startPlay?subBiz=mainApp&kpn=ACFUN_APP&kpf=PC_WEB";
                var response = await _acfunApiRequest.Post(requesturl, form);
                var result = JsonConvert.DeserializeObject<JToken>(response)?["result"]?.ToString();
                if (result is null || result != "1")
                {
                    _logger.LogError("Acfun获取录播链接失败");
                    return false;
                }

                var responseObject = JsonConvert.DeserializeObject<GetPlaybackResponse>(response);
                var adaptiveManifest = JsonConvert.DeserializeObject<AdaptiveManifest>(responseObject.data.adaptiveManifest);

                // 获取第一个录播链接并写入数据库
                if (adaptiveManifest.adaptationSet.Any() &&
                    adaptiveManifest.adaptationSet[0].representation.Any())
                {
                    // 获取主录播链接
                    var mainUrl = adaptiveManifest.adaptationSet[0].representation[0].url;
                    latestLive.url = mainUrl;

                    // 获取备用录播链接（如果有）
                    if (adaptiveManifest.adaptationSet[0].representation[0].backupUrl.Any())
                    {
                        latestLive.url_backup = adaptiveManifest.adaptationSet[0].representation[0].backupUrl[0];
                    }

                    // 保存到数据库
                    _dbContext.Update(latestLive);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Acfun录播链接已保存到数据库: {latestLive.streamName}");
                    return true;
                }
                else
                {
                    _logger.LogError("Acfun未找到有效的录播链接");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Acfun获取录播链接失败");
                return false;
            }
        }

    }
}
