using LiveRecorder.NET.IServices;
using LiveRecorder.NET.Models;
using LiveRecorder.NET.Services.Singleton;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Plugins
{
    class Twitcasting : IWebsiteService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Twitcasting> _logger;
        private readonly RecordingWSService _recordingService;
        private readonly IConfiguration _configuration;

        public Twitcasting(IHttpClientFactory httpClientFactory, ILogger<Twitcasting> logger, RecordingWSService recordingService, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _recordingService = recordingService;
            _configuration = configuration;
        }
        public async Task<int> CheckLiveStatus(Streamer streamer)
        {
            string url = $"https://frontendapi.twitcasting.tv/watch/user/{streamer.Channel}";
            #region 设置header
            var headers = new Dictionary<string, string>();
            var accounts = _configuration.GetSection("accounts");
            if (accounts is not null)
            {
                var account = accounts.GetValue<string>("twitcasting_username");
                var token = accounts.GetValue<string>("twitcasting_token");
                if (!string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(token))
                {
                    headers.Add("Cookie", $"tc_id={account};tc_ss={token};");
                }
            }
            if (!string.IsNullOrEmpty(streamer.LivePassword))
            {
                headers.Add("wpass", $"{CalculatePasswordHash(streamer.LivePassword)}");
            }
            headers.Add("Origin", "https://twitcasting.tv/");
            headers.Add("Referer", $"https://twitcasting.tv/{streamer.Channel}");
            streamer.CustomHeader = headers;
            #endregion 设置header
            var body = new
            {
                userId = streamer.Channel
            };
            string jsonString = JsonSerializer.Serialize(body);
            HttpClient client = _httpClientFactory.CreateClient();

            try
            {
                if (streamer.CustomHeader is not null && streamer.CustomHeader.Count > 0)
                {
                    foreach (var header in streamer.CustomHeader)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var response = await client.PostAsync(url, new StringContent(jsonString, Encoding.UTF8, "application/json"));
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return 0;
                }
                var responseBody = await response.Content.ReadAsStringAsync();
                var resObj = JsonNode.Parse(responseBody);
                var isLiveNode = resObj?["is_live"];
                if (isLiveNode is not null)
                {
                    var isLive = isLiveNode.GetValue<bool>();
                    if (isLive)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{name}查询直播状态失败", streamer.Name);
                if (ex.InnerException is not null)
                {
                    _logger.LogTrace(ex.InnerException.Message);
                }
                _logger.LogTrace(ex.Message + "\n" + ex.StackTrace);
            }


            return 0;
        }

        public async Task<string> GetLiveUrl(Streamer streamer)
        {
            string url = $"https://twitcasting.tv/streamserver.php?target={streamer.Channel}&mode=client&player=pc_web";

            HttpClient client = _httpClientFactory.CreateClient();
            try
            {
                if (streamer.CustomHeader is not null && streamer.CustomHeader.Count > 0)
                {
                    foreach (var header in streamer.CustomHeader)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var response = await client.GetAsync(url);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return "";
                }
                var responseBody = await response.Content.ReadAsStringAsync();
                var resObj = JsonNode.Parse(responseBody);
                var streamUrlNode = resObj?["llfmp4"]?["streams"];
                var streamUrl = "";
                if (streamUrlNode is not null)
                {
                    // 按照优先级尝试获取流地址（高 > 中 > 低）
                    if (streamUrlNode["main"] != null && !string.IsNullOrEmpty(streamUrlNode["main"]?.GetValue<string>()))
                    {
                        streamUrl = streamUrlNode["main"]?.GetValue<string>();
                    }
                    else if (streamUrlNode["mobilesource"] != null && !string.IsNullOrEmpty(streamUrlNode["mobilesource"]?.GetValue<string>()))
                    {
                        streamUrl = streamUrlNode["mobilesource"]?.GetValue<string>();
                    }
                    else if (streamUrlNode["base"] != null && !string.IsNullOrEmpty(streamUrlNode["base"]?.GetValue<string>()))
                    {
                        streamUrl = streamUrlNode["base"]?.GetValue<string>();
                    }
                }
                if (!string.IsNullOrEmpty(streamUrl))
                {
                    return streamUrl;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("{name}获取直播地址失败", streamer.Name);
                if (ex.InnerException is not null)
                {
                    _logger.LogTrace(ex.InnerException.Message);
                }
                _logger.LogTrace(ex.Message + "\n" + ex.StackTrace);
            }

            return "";
        }

        public async Task<bool> StartRecording(Streamer streamer)
        {
            var url = await GetLiveUrl(streamer);
            return _recordingService.StartRecording(url, streamer);
        }
        public async Task<bool> EndRecording(Streamer streamer)
        {
            return true;
        }
        string CalculatePasswordHash(string password)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(password);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // 将字节数组转换为十六进制字符串
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }
    }
}

