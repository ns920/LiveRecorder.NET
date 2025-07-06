using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Services
{
    /// <summary>
    /// Discord服务类，提供消息发送和回复功能
    /// </summary>
    public class DiscordService
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _services;
        private readonly ILogger<DiscordService> _logger;
        private readonly IConfiguration _configuration;

        public DiscordService(DiscordSocketClient client, CommandService commandService, IServiceProvider services, ILogger<DiscordService> logger,IConfiguration configuration)
        {
            _client = client;
            _commandService = commandService;
            _services = services;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// 初始化Discord服务，注册命令模块并设置事件处理
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                // 注册当前程序集中的所有命令模块
                await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

                // 订阅消息事件
                _client.MessageReceived += HandleCommandAsync;

                _logger.LogInformation("Discord服务初始化完成，已注册 {ModuleCount} 个命令模块",
                    _commandService.Modules.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Discord服务初始化失败");
                throw;
            }
        }

        /// <summary>
        /// 处理接收到的消息，检查是否为命令
        /// </summary>
        private async Task HandleCommandAsync(SocketMessage messageParam)
        {
            // 确保消息来自用户而不是机器人
            if (messageParam is not SocketUserMessage message || message.Author.IsBot)
                return;

            int argPos = 0;

            // 检查消息是否以指定前缀开始或者是否提及了机器人
            if (!(message.HasCharPrefix('!', ref argPos) ||
                  message.HasMentionPrefix(_client.CurrentUser, ref argPos)))
                return;

            // 创建命令上下文
            var context = new SocketCommandContext(_client, message);

            try
            {
                // 执行命令
                var result = await _commandService.ExecuteAsync(context, argPos, _services);

                // 如果命令执行失败且不是未知命令错误，记录错误
                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    _logger.LogWarning("命令执行失败: {Error} - {ErrorReason}",
                        result.Error, result.ErrorReason);

                    // 可选：向用户发送错误信息
                    await context.Channel.SendMessageAsync($"❌ 命令执行失败: {result.ErrorReason}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理命令时发生异常");
            }
        }

        /// <summary>
        /// 主动发送消息到指定频道
        /// </summary>
        /// <param name="channelId">频道ID</param>
        /// <param name="message">要发送的消息内容</param>
        /// <param name="embed">可选的嵌入消息</param>
        /// <returns>发送的消息</returns>
        public async Task<IUserMessage> SendMessageAsync(ulong channelId, string message = null, Embed embed = null)
        {
            try
            {
                if (string.IsNullOrEmpty(message) && embed == null)
                {
                    throw new ArgumentException("消息内容和嵌入消息不能同时为空");
                }

                var channel = _client.GetChannel(channelId) as IMessageChannel;
                if (channel == null)
                {
                    throw new InvalidOperationException($"无法找到频道 ID: {channelId}");
                }

                _logger.LogDebug("正在发送消息到频道 {ChannelName} ({ChannelId})",
                    channel.Name, channelId);

                var sentMessage = await channel.SendMessageAsync(
                    text: message,
                    embed: embed
                );

                _logger.LogInformation("成功发送消息到频道 {ChannelName}", channel.Name);
                return sentMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息到频道 {ChannelId} 失败", channelId);
                throw;
            }
        }

        /// <summary>
        /// 主动发送消息到指定频道（通过频道对象）
        /// </summary>
        /// <param name="channel">目标频道</param>
        /// <param name="message">要发送的消息内容</param>
        /// <param name="embed">可选的嵌入消息</param>
        /// <returns>发送的消息</returns>
        public async Task<IUserMessage> SendMessageAsync(IMessageChannel channel, string message = null, Embed embed = null)
        {
            try
            {
                if (channel == null)
                {
                    throw new ArgumentNullException(nameof(channel));
                }

                if (string.IsNullOrEmpty(message) && embed == null)
                {
                    throw new ArgumentException("消息内容和嵌入消息不能同时为空");
                }

                _logger.LogDebug("正在发送消息到频道 {ChannelName} ({ChannelId})",
                    channel.Name, channel.Id);

                var sentMessage = await channel.SendMessageAsync(
                    text: message,
                    embed: embed
                );

                _logger.LogInformation("成功发送消息到频道 {ChannelName}", channel.Name);
                return sentMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息到频道 {ChannelName} 失败", channel.Name);
                throw;
            }
        }

        /// <summary>
        /// 发送私人消息给指定用户
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="message">要发送的消息内容</param>
        /// <param name="embed">可选的嵌入消息</param>
        /// <returns>发送的消息</returns>
        public async Task<IUserMessage> SendDirectMessageAsync(ulong userId, string message = null, Embed embed = null)
        {
            try
            {
                if (string.IsNullOrEmpty(message) && embed == null)
                {
                    throw new ArgumentException("消息内容和嵌入消息不能同时为空");
                }
                var user = await _client.GetUserAsync(userId, null);
                if (user == null)
                {
                    throw new InvalidOperationException($"无法找到用户 ID: {userId}");
                }

                _logger.LogDebug("正在发送私人消息给用户 {Username} ({UserId})",
                    user.Username, userId);

                var dmChannel = await user.CreateDMChannelAsync();
                var sentMessage = await dmChannel.SendMessageAsync(
                    text: message,
                    embed: embed
                );

                _logger.LogInformation("成功发送私人消息给用户 {Username}", user.Username);
                return sentMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送私人消息给用户 {UserId} 失败", userId);
                throw;
            }
        }

        public async Task ReLoginAsync()
        {
            try
            {
                var token = _configuration["accounts:discord_token"];
                if (!string.IsNullOrEmpty(token)&&_client.ConnectionState==ConnectionState.Disconnected)
                {

                    Log.Information("正在重新连接Discord客户端...");

                    await _client.LoginAsync(TokenType.Bot, token);
                    await _client.StartAsync();
                    Log.Information("Discord客户端启动成功");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Discord客户端启动失败");
                throw;
            }
        }
        /// <summary>
        /// 服务是否可用
        /// </summary>
        public bool IsAvailable => _client.ConnectionState == ConnectionState.Connected;
    }
}