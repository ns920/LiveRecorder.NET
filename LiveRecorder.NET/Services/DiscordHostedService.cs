using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Services
{
    public class DiscordHostedService : IHostedService
    {
        private readonly DiscordSocketClient _client;
        private readonly IConfiguration _configuration;

        public DiscordHostedService(DiscordSocketClient client, IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var token = _configuration["accounts:discord_token"];
                if (!string.IsNullOrEmpty(token))
                {

                    Log.Information("正在启动Discord客户端...");

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

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                Log.Information("正在停止Discord客户端...");
                await _client.StopAsync();
                await _client.LogoutAsync();
                Log.Information("Discord客户端已停止");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "停止Discord客户端时发生错误");
            }
        }
    }
}
