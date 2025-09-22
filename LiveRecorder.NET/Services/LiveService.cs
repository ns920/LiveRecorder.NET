using LiveRecorder.NET.IServices;
using LiveRecorder.NET.Models;
using LiveRecorder.NET.Models.Enum;
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
                     existing.LivePassword != config.LivePassword ||
                     existing.IsNotify != config.IsNotify ||
                     existing.IsRecord != config.IsRecord
                     ))).ToList();

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

                lock (existingStreamer)
                {
                    // 更新所有其他属性
                    existingStreamer.Name = updatedStreamer.Name;
                    existingStreamer.UserName = updatedStreamer.UserName;
                    existingStreamer.Password = updatedStreamer.Password;
                    existingStreamer.Token = updatedStreamer.Token;
                    existingStreamer.LivePassword = updatedStreamer.LivePassword;
                    existingStreamer.IsNotify = updatedStreamer.IsNotify;
                    existingStreamer.IsRecord = updatedStreamer.IsRecord;
                }
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
                // 统一的状态检查逻辑
                bool shouldSkip = false;

                if (streamer.Type == "acfun")
                {
                    // acfun类型：只有在检查中时跳过
                    if (streamer.Status == StreamerStatus.Checking)
                    {
                        shouldSkip = true;
                    }
                }
                else
                {
                    // 其他平台：检查中、录制中时跳过
                    if (streamer.Status == StreamerStatus.Checking ||
                        streamer.Status == StreamerStatus.Recording)
                    {
                        shouldSkip = true;
                    }
                }

                if (shouldSkip)
                {
                    continue;
                }

                // 同步设置为检查中状态，防止重复触发
                var previousStatus = streamer.Status;
                streamer.Status = StreamerStatus.Checking;

                // 创建并启动处理此主播的任务
                var task = Task.Run(async () =>
                {
                    bool isLive = await CheckIfLive(streamer, stoppingToken);

                    if (isLive)
                    {
                        // 只在状态从非直播状态变为直播状态时记录开播消息
                        if (previousStatus == StreamerStatus.Offline)
                        {
                            _logger.LogInformation("{name} ({channel})开播了", streamer.Name, streamer.Channel);

                            // 根据IsNotify判断是否发送通知
                            if (streamer.IsNotify)
                            {
                                var discordId = _configuration.GetValue<ulong>("accounts:discord_userid", 0);
                                if (discordId > 0 && _discordService.IsAvailable && !streamer.MessageSend)
                                {
                                    await _discordService.SendDirectMessageAsync(discordId, $"{streamer.Name} ({streamer.Channel})开播了！");
                                    streamer.MessageSend = true;
                                }
                            }
                        }

                        // 如果配置需要录制，则走录制流程；否则更新状态为Living
                        if (streamer.IsRecord)
                        {
                            // 只有从非录制状态变为录制状态时才启动录制
                            if (previousStatus != StreamerStatus.Recording)
                            {
                                streamer.Status = StreamerStatus.Recording;
                                _logger.LogInformation("开始录制 {name} ({channel}) 的直播", streamer.Name, streamer.Channel);
                                var start = await _websiteServiceFactory(streamer.Type).StartRecording(streamer);
                            }
                            else
                            {
                                // 保持录制状态
                                streamer.Status = StreamerStatus.Recording;
                            }
                        }
                        else
                        {
                            // 不录制的情况下设置为Living状态
                            streamer.Status = StreamerStatus.Living;
                            if (previousStatus == StreamerStatus.Offline)
                            {
                                _logger.LogInformation("主播 {name} ({channel})已开播，但录制功能已禁用", streamer.Name, streamer.Channel);
                            }
                        }
                    }
                    else
                    {
                        // 检测到主播下播
                        if (previousStatus == StreamerStatus.Living || previousStatus == StreamerStatus.Recording)
                        {
                            _logger.LogInformation("{name} ({channel})直播结束了", streamer.Name, streamer.Channel);

                            // 如果之前在录制，需要结束录制
                            if (previousStatus == StreamerStatus.Recording)
                            {
                                await _websiteServiceFactory(streamer.Type).EndRecording(streamer);
                            }
                        }

                        // 重置消息发送状态和主播状态
                        streamer.MessageSend = false;
                        streamer.Status = StreamerStatus.Offline;
                    }

                    // 如果状态仍然是检查中，说明出现了异常，重置为离线状态
                    if (streamer.Status == StreamerStatus.Checking)
                    {
                        streamer.Status = StreamerStatus.Offline;
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
            Task.Run(async () => await RecordingCompleted(e.Streamer));
        }

        /// <summary>
        /// 处理录制完成事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnRecordingCompletedWS(object? sender, RecordingWSService.RecordingCompletedEventArgs e)
        {
            Task.Run(async () => await RecordingCompleted(e.Streamer));
        }

        private async Task RecordingCompleted(Streamer streamer)
        {
            if (streamer.Status == StreamerStatus.Recording)
            {
                _logger.LogInformation("{name} ({channel})直播结束了", streamer.Name, streamer.Channel);
                await _websiteServiceFactory(streamer.Type).EndRecording(streamer);
            }

            streamer.Status = StreamerStatus.Offline;
        }
    }
}
