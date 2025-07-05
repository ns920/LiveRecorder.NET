using LiveRecorder.NET.IServices;
using LiveRecorder.NET.Models;
using LiveRecorder.NET.Services.Singleton;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Services
{
    class LiveService : BackgroundService
    {
        private readonly ILogger<LiveService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Func<string, IWebsiteService> _websiteServiceFactory;
        private readonly RecordService _recordService;
        private readonly RecordingWSService _recordingWSService;
        private readonly DiscordService _discordService;
        private readonly List<Streamer> _streamers = new List<Streamer>();
        private int _pollingInterval;

        public LiveService(ILogger<LiveService> logger, IConfiguration configuration
            , Func<string, IWebsiteService> websiteServiceFactory,
            RecordService recordService,
            RecordingWSService recordingWSService,
            DiscordService discordService)
        {
            _logger = logger;
            _configuration = configuration;
            _websiteServiceFactory = websiteServiceFactory;
            _recordService = recordService;
            _pollingInterval = _configuration.GetValue<int>("interval", 10000); // 默认值为10秒
            _recordingWSService = recordingWSService;
            _discordService = discordService;

            // 初始化直播者列表
            var streamerConfigs = _configuration.GetSection("streamers").Get<List<Streamer>>();
            if (streamerConfigs != null)
            {
                _streamers.AddRange(streamerConfigs);
            }

            _recordService.RecordingCompleted += OnRecordingCompleted;
            _recordingWSService.RecordingCompleted += OnRecordingCompletedWS;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Thread.Sleep(3000);
            _logger.LogInformation("LiveService 开始运行");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // 更新轮询间隔
                    _pollingInterval = _configuration.GetValue<int>("interval", 10000);
                    UpdateStreamers();
                    await CheckStreamers(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检查直播状态时发生错误");
                }

                await Task.Delay(_pollingInterval, stoppingToken);
            }

            _logger.LogInformation("LiveService 已停止");
        }

        /// <summary>
        /// 从配置文件更新直播者列表
        /// </summary>
        private void UpdateStreamers()
        {
            // 从配置文件获取最新的直播者列表
            var configStreamers = _configuration.GetSection("streamers").Get<List<Streamer>>() ?? new List<Streamer>();

            // 找出需要删除的直播者（在当前列表中但不在配置文件中的）
            var streamersToRemove = _streamers.Where(existing =>
                !configStreamers.Any(config =>
                    config.Channel == existing.Channel &&
                    config.Type == existing.Type)).ToList();

            // 找出需要添加的新直播者（在配置文件中但不在当前列表中的）
            var streamersToAdd = configStreamers.Where(config =>
                !_streamers.Any(existing =>
                    existing.Channel == config.Channel &&
                    existing.Type == config.Type)).ToList();

            // 找出信息已更新的直播者
            var streamersToUpdate = configStreamers.Where(config =>
                _streamers.Any(existing =>
                    existing.Channel == config.Channel &&
                    existing.Type == config.Type &&
                    (existing.Name != config.Name ||
                     existing.UserName != config.UserName ||
                     existing.Password != config.Password ||
                     existing.Token != config.Token ||
                     existing.LivePassword != config.LivePassword))).ToList();

            // 从列表中移除不再存在的直播者
            foreach (var streamer in streamersToRemove)
            {
                _logger.LogInformation("移除直播者: {name} ({channel})", streamer.Name, streamer.Channel);
                _streamers.Remove(streamer);
            }

            // 添加新的直播者
            foreach (var streamer in streamersToAdd)
            {
                _logger.LogInformation("添加新直播者: {name} ({channel})", streamer.Name, streamer.Channel);
                _streamers.Add(streamer);
            }

            // 更新已有直播者的信息
            foreach (var updatedStreamer in streamersToUpdate)
            {
                var existingStreamer = _streamers.First(s =>
                    s.Channel == updatedStreamer.Channel &&
                    s.Type == updatedStreamer.Type);

                _logger.LogInformation("更新直播者信息: {name} ({channel})", existingStreamer.Name, existingStreamer.Channel);

                // 保留当前直播状态
                int currentStatus = existingStreamer.Status;

                // 更新所有其他属性
                existingStreamer.Name = updatedStreamer.Name;
                existingStreamer.UserName = updatedStreamer.UserName;
                existingStreamer.Password = updatedStreamer.Password;
                existingStreamer.Token = updatedStreamer.Token;
                existingStreamer.LivePassword = updatedStreamer.LivePassword;

                // 确保状态保持不变
                existingStreamer.Status = currentStatus;
            }
        }
        private async Task CheckStreamers(CancellationToken stoppingToken)
        {
            if (_streamers == null || !_streamers.Any())
            {
                return;
            }

            var tasks = new List<Task>();

            foreach (var streamer in _streamers)
            {

                // 创建并启动处理此主播的任务
                var task = Task.Run(async () =>
                {
                    bool isLive = await CheckIfLive(streamer, stoppingToken);

                    var oldStatus = streamer.Status; // 记录旧状态
                    if (isLive)
                    {
                        if (oldStatus != 1)
                        {
                            streamer.Status = 1; // 更新状态为直播中
                            _logger.LogInformation("{name} ({channel})开播了", streamer.Name, streamer.Channel);
                            _logger.LogInformation("开始录制 {name} ({channel}) 的直播", streamer.Name, streamer.Channel);
                            var discordId = _configuration.GetValue<ulong>("accounts:discord_userid", 0);
                            if (discordId > 0 && _discordService.IsAvailable&&!streamer.MessageSend)
                            {
                                await _discordService.SendDirectMessageAsync(discordId, $"{streamer.Name} ({streamer.Channel})开播了！");
                                streamer.MessageSend = true; 
                            }
                            var start = await _websiteServiceFactory(streamer.Type).StartRecording(streamer);
                        }
                    }
                    else
                    {
                        streamer.MessageSend = false;
                        streamer.Status = 0;
                        if (oldStatus == 1)
                        {
                            _logger.LogInformation("{name} ({channel})直播结束了", streamer.Name, streamer.Channel);
                            await _websiteServiceFactory(streamer.Type).EndRecording(streamer);
                        }
                    }
                }, stoppingToken);

                tasks.Add(task);

                if (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(200, stoppingToken);
                }
            }

            // 等待所有任务完成
            await Task.WhenAll(tasks);
        }

        private async Task<bool> CheckIfLive(Streamer streamer, CancellationToken stoppingToken)
        {
            // 检查直播状态的逻辑
            _logger.LogDebug("检查直播者 {name} ({channel}) 的直播状态", streamer.Name, streamer.Channel);

            // 获取直播平台类型（转换为小写以便不区分大小写匹配）
            string platformType = streamer.Type.ToLowerInvariant();

            // 使用工厂获取对应平台的服务实现
            var service = _websiteServiceFactory(platformType);

            if (service != null)
            {
                try
                {
                    // 调用对应服务的CheckLiveStatus方法
                    int liveStatus = await service.CheckLiveStatus(streamer);

                    // 解析返回结果：1表示直播中，0表示未直播
                    return liveStatus == 1;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "检查 {platform} 平台 {channel} 的直播状态时发生错误",
                        streamer.Type, streamer.Channel);
                    return false; // 发生错误时默认为未直播
                }
            }
            else
            {
                _logger.LogWarning("未找到平台 {platform} ", streamer.Type);
                return false; // 未找到对应服务时默认为未直播
            }
        }

        /// <summary>
        /// 处理录制完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRecordingCompleted(object? sender, RecordService.RecordingCompletedEventArgs e)
        {
            e.Streamer.Status = 0;
        }

        /// <summary>
        /// 处理录制完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRecordingCompletedWS(object? sender, RecordingWSService.RecordingCompletedEventArgs e)
        {
            e.Streamer.Status = 0;
        }
    }
}
