using AcfunApi;
using Discord;
using Discord.Commands;
using Discord.Net.Rest;
using Discord.Net.WebSockets;
using Discord.WebSocket;
using LiveRecorder.NET.Data;
using LiveRecorder.NET.IServices;
using LiveRecorder.NET.Services;
using LiveRecorder.NET.Services.Singleton;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace LiveRecorder.NET
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 读取配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // 配置Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                Log.Information("应用程序启动中...");

                var host = CreateHostBuilder(args).Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "应用程序启动失败");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .UseSerilog() // 使用Serilog作为日志提供程序
                .ConfigureServices(async (hostContext, services) =>
                {
                    ConfigureProxy(hostContext, services); // 配置代理
                    ConfigureDataBase(hostContext, services);//配置数据库
                    // 注册所有实现IWebsiteService的插件
                    RegisterWebsiteServices(services);
                    RegisterDiscordServices(services);
                    services.AddHostedService<LiveService>();
                    services.AddSingleton<AcfunApiRequest>();
                    RegisterSingletonServices(services);

                });


        // 通过反射注册所有IWebsiteService实现
        private static void RegisterWebsiteServices(IServiceCollection services)
        {
            // 获取程序集
            var assembly = typeof(Program).Assembly;

            // 查找所有实现IWebsiteService的类型
            var serviceTypes = assembly.GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract &&
                            typeof(IWebsiteService).IsAssignableFrom(t));

            // 创建一个字典，用于存储服务名称和服务类型的映射关系
            var serviceTypeMap = new Dictionary<string, Type>();

            foreach (var type in serviceTypes)
            {
                // 获取该类型的服务名称（例如：Twitcasting类的名称为"twitcasting"）
                var serviceName = type.Name.ToLowerInvariant();

                // 注册服务实现为它自己的类型
                services.AddSingleton(type);

                // 同时注册为IWebsiteService接口
                services.AddSingleton(typeof(IWebsiteService), type);

                // 添加到映射字典
                serviceTypeMap[serviceName] = type;
            }

            // 注册服务工厂
            services.AddSingleton<Func<string, IWebsiteService>>(serviceProvider => platformType =>
            {
                if (serviceTypeMap.TryGetValue(platformType.ToLowerInvariant(), out var serviceType))
                {
                    // 使用服务提供者获取对应类型的服务实例
                    return (IWebsiteService)serviceProvider.GetService(serviceType);
                }
                return null;
            });
        }
        // 通过反射注册所有 LiveRecorder.NET.Services.Singleton 命名空间下的服务
        private static void RegisterSingletonServices(IServiceCollection services)
        {
            // 获取程序集
            var assembly = typeof(Program).Assembly;

            // 查找 LiveRecorder.NET.Services.Singleton 命名空间下的所有类型
            var serviceTypes = assembly.GetTypes()
                .Where(t => !t.IsInterface && !t.IsAbstract &&
                            t.Namespace == "LiveRecorder.NET.Services.Singleton");

            foreach (var type in serviceTypes)
            {
                // 将所有服务注册为单例
                services.AddSingleton(type);
            }

        }
        private static void RegisterDiscordServices(IServiceCollection services)
        {
            // 注册Discord相关服务
            services.AddSingleton<DiscordLogService>();
            // 配置 DiscordSocketClient 使用代理
            services.AddSingleton<DiscordSocketClient>(provider =>
            {
                var configuration = provider.GetRequiredService<IConfiguration>();
                var proxyUrl = configuration["proxy"];

                var config = new DiscordSocketConfig();

                // 如果配置了代理，则为 Discord 客户端设置代理
                if (!string.IsNullOrEmpty(proxyUrl))
                {
                    config.RestClientProvider = DefaultRestClientProvider.Create(useProxy: true);
                    config.WebSocketProvider = DefaultWebSocketProvider.Create(new WebProxy(proxyUrl));
                }

                return new DiscordSocketClient(config);
            });
            services.AddSingleton<CommandService>();
            services.AddSingleton<DiscordService>();
            services.AddHostedService<DiscordHostedService>();
        }
        private static void ConfigureProxy(HostBuilderContext hostContext, IServiceCollection services)
        {
            // 获取代理配置
            var proxyUrl = hostContext.Configuration["proxy"];
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                Log.Information("使用全局代理：{proxyUrl}", proxyUrl);

                // 为所有 HttpClient 配置默认设置
                services.ConfigureHttpClientDefaults(builder =>
                {
                    builder.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                    {
                        Proxy = new WebProxy(proxyUrl),
                        UseProxy = true
                    });
                });

                // 仍然添加 HttpClient 工厂
                services.AddHttpClient();

            }
            else
            {
                // 如果没有代理配置，仍然添加HttpClient工厂但不设置代理
                services.AddHttpClient();
            }
        }

        private static void ConfigureDataBase(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddDbContext<AcfunlivedbDbContext>(options =>
            {
                SQLitePCL.Batteries.Init();
                options.UseSqlite("Data Source=./acfunlive.db;");
            });
            services.AddScoped<AutoMigrations>();
            //DI获取迁移类
            IServiceProvider provider = services.BuildServiceProvider();
            AutoMigrations autoMigrations = provider.GetRequiredService<AutoMigrations>();
            //自动执行迁移
            autoMigrations.CommitMigrations();
        }

    }
}
