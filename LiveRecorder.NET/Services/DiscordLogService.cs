using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Services
{
    /// <summary>
    /// Discord机器人日志服务，使用Serilog记录Discord.NET的日志消息
    /// </summary>
    public class DiscordLogService
    {
        private readonly ILogger<DiscordLogService> _logger;

        public DiscordLogService(ILogger<DiscordLogService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 初始化Discord日志服务，订阅Discord客户端和命令服务的日志事件
        /// </summary>
        /// <param name="client">Discord Socket客户端</param>
        /// <param name="commandService">Discord命令服务（可选）</param>
        public void Initialize(DiscordSocketClient client, CommandService commandService = null)
        {
            if (client != null)
            {
                client.Log += LogDiscordMessageAsync;
                _logger.LogDebug("已订阅Discord客户端日志事件");
            }

            if (commandService != null)
            {
                commandService.Log += LogDiscordMessageAsync;
                _logger.LogDebug("已订阅Discord命令服务日志事件");
            }
        }

        /// <summary>
        /// 处理Discord.NET的日志消息
        /// </summary>
        /// <param name="message">Discord日志消息</param>
        /// <returns>完成的任务</returns>
        private Task LogDiscordMessageAsync(LogMessage message)
        {
            try
            {
                // 处理命令异常
                if (message.Exception is CommandException cmdException)
                {
                    var commandName = cmdException.Command?.Aliases?.FirstOrDefault() ?? "未知命令";
                    var channelInfo = cmdException.Context?.Channel?.Name ?? "未知频道";

                    Log.Debug("[Discord命令/{Severity}] 命令 {CommandName} 在频道 {Channel} 中执行失败: {Exception}",
                        message.Severity, commandName, channelInfo, cmdException.Message);

                    if (cmdException.InnerException != null)
                    {
                        Log.Debug("[Discord命令异常详情] {Exception}", cmdException.InnerException);
                    }
                }
                else
                {
                    // 根据Discord日志级别映射到Serilog级别
                    switch (message.Severity)
                    {
                        case LogSeverity.Critical:
                            Log.Fatal("[Discord/{Severity}] {Source}: {Message} {Exception}",
                                message.Severity, message.Source, message.Message, message.Exception);
                            break;
                        case LogSeverity.Error:
                            Log.Error("[Discord/{Severity}] {Source}: {Message} {Exception}",
                                message.Severity, message.Source, message.Message, message.Exception);
                            break;
                        case LogSeverity.Warning:
                            Log.Warning("[Discord/{Severity}] {Source}: {Message}",
                                message.Severity, message.Source, message.Message);
                            break;
                        case LogSeverity.Info:
                            Log.Information("[Discord/{Severity}] {Source}: {Message}",
                                message.Severity, message.Source, message.Message);
                            break;
                        case LogSeverity.Verbose:
                        case LogSeverity.Debug:
                        default:
                            Log.Debug("[Discord/{Severity}] {Source}: {Message}",
                                message.Severity, message.Source, message.Message);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                // 防止日志记录过程中出现异常导致程序崩溃
                Log.Error(ex, "处理Discord日志消息时发生错误");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 记录Discord连接状态变化
        /// </summary>
        /// <param name="client">Discord客户端</param>
        public void LogConnectionState(DiscordSocketClient client)
        {
            if (client != null)
            {
                Log.Debug("[Discord连接] 当前连接状态: {ConnectionState}, 延迟: {Latency}ms",
                    client.ConnectionState, client.Latency);
            }
        }

        /// <summary>
        /// 记录Discord用户活动
        /// </summary>
        /// <param name="user">用户信息</param>
        /// <param name="action">用户操作</param>
        public void LogUserActivity(IUser user, string action)
        {
            if (user != null)
            {
                Log.Debug("[Discord用户活动] 用户 {Username}#{Discriminator} ({UserId}) {Action}",
                    user.Username, user.Discriminator, user.Id, action);
            }
        }

        /// <summary>
        /// 记录Discord服务器活动
        /// </summary>
        /// <param name="guild">服务器信息</param>
        /// <param name="action">服务器操作</param>
        public void LogGuildActivity(IGuild guild, string action)
        {
            if (guild != null)
            {
                Log.Debug("[Discord服务器活动] 服务器 {GuildName} ({GuildId}) {Action}",
                    guild.Name, guild.Id, action);
            }
        }
    }
}