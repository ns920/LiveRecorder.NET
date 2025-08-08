using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFMpegCore;
using LiveRecorder.NET.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LiveRecorder.NET.Services.Singleton
{
    public class RecordingWSService
    {
        private readonly ILogger<RecordingWSService> _logger;
        private readonly IConfiguration _configuration;
        private readonly List<CancellationTokenSource> _activeRecordings = new List<CancellationTokenSource>();
        private readonly object _recordingsLock = new object();
        private readonly List<Task> _activeFFmpegTasks = new List<Task>();
        private readonly object _ffmpegTasksLock = new object();

        // 定义录制完成事件
        public event EventHandler<RecordingCompletedEventArgs> RecordingCompleted;

        // 创建事件参数类
        public class RecordingCompletedEventArgs : EventArgs
        {
            public Streamer Streamer { get; }

            public RecordingCompletedEventArgs(Streamer streamer)
            {
                Streamer = streamer;
            }
        }

        public RecordingWSService(ILogger<RecordingWSService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // 注册应用程序退出事件
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        public bool StartRecording(string url, Streamer streamer)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("WebSocket链接地址为空，无法开始录制");
                    return false;
                }

                // 创建录制目录
                string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, streamer.Name);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 生成输出文件名（使用时间戳）
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string outputFilePath = Path.Combine(directoryPath, $"{timestamp}.mp4");

                // 创建取消令牌
                var cts = new CancellationTokenSource();

                // 将取消令牌添加到活跃录制列表
                lock (_recordingsLock)
                {
                    _activeRecordings.Add(cts);
                }

                // 异步启动以避免阻塞当前线程
                Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation($"正在连接WebSocket: {url}");

                        // 创建WebSocket客户端
                        using var client = new ClientWebSocket();

                        // 配置代理（如果有）
                        var proxyUrl = _configuration["proxy"];
                        if (!string.IsNullOrEmpty(proxyUrl))
                        {
                            _logger.LogInformation($"使用代理: {proxyUrl}");
                            client.Options.Proxy = new System.Net.WebProxy(proxyUrl);
                        }

                        var startTime = DateTime.Now;

                        // 设置合理的超时时间
                        var connectTimeout = TimeSpan.FromSeconds(30);
                        using var connectCts = new CancellationTokenSource(connectTimeout);
                        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                            connectCts.Token, cts.Token);

                        // 在连接之前添加必要的头信息和子协议
                        client.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

                        if (streamer.CustomHeader.Count > 0)
                        {
                            foreach (var header in streamer.CustomHeader)
                            {
                                client.Options.SetRequestHeader(header.Key, header.Value);
                            }
                        }

                        using var memoryStream = new MemoryStream();
                        int bufferSize = 1024 * 16;
                        var receiveBuffer = new byte[bufferSize];
                        var totalBytesReceived = 0L;

                        _logger.LogInformation($"连接到WebSocket服务器: {url}");
                        try
                        {
                            await client.ConnectAsync(new Uri(url), combinedCts.Token);
                            _logger.LogInformation("WebSocket连接成功");
                        }
                        catch (WebSocketException wsEx)
                        {
                            _logger.LogError("{streamer}连接WebSocket服务器失败", streamer.Name);
                        }

                        if (client.State == WebSocketState.Open)
                        {
                            // 创建文件流
                            using var fileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write);
                            // 接收数据直到连接关闭或取消
                            while (client.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
                            {
                                WebSocketReceiveResult result;
                                try
                                {
                                    // 接收一个消息片段
                                    result = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cts.Token);

                                    if (result.MessageType == WebSocketMessageType.Close)
                                    {
                                        _logger.LogTrace("收到WebSocket关闭消息");
                                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "完成", CancellationToken.None);
                                        break;
                                    }

                                    // 将片段写入内存流
                                    await memoryStream.WriteAsync(receiveBuffer, 0, result.Count, cts.Token);
                                    totalBytesReceived += result.Count;

                                    // 如果消息接收完成，则写入文件并重置内存流
                                    if (result.EndOfMessage)
                                    {
                                        // 将完整消息写入文件
                                        memoryStream.Position = 0;
                                        await memoryStream.CopyToAsync(fileStream, bufferSize, cts.Token);
                                        await fileStream.FlushAsync(cts.Token);

                                        // 重置内存流
                                        memoryStream.SetLength(0);

                                        // 记录接收到完整消息
                                        _logger.LogTrace("接收到完整消息，大小: {0} 字节", totalBytesReceived);
                                    }
                                }
                                catch (WebSocketException wsEx)
                                {
                                    _logger.LogError(wsEx, "WebSocket接收数据时出错");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "处理WebSocket数据时出错");
                                    break;
                                }
                            }


                            // 最后刷新确保所有数据写入磁盘
                            await fileStream.FlushAsync(CancellationToken.None);
                        }


                        // 检查录制结果
                        var fileInfo = new FileInfo(outputFilePath);
                        if (File.Exists(outputFilePath) && fileInfo.Length > 0)
                        {
                            var duration = DateTime.Now - startTime;
                            var sizeMB = fileInfo.Length / (1024.0 * 1024.0);

                            _logger.LogInformation(
                                "WebSocket录制完成: {0}, 文件大小: {1:F2} MB, 录制时长: {2:hh\\:mm\\:ss}",
                                streamer.Name, sizeMB, duration);

                        }
                        else
                        {
                            _logger.LogError($"WebSocket录制失败或没有数据: {streamer.Name}");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning($"WebSocket录制已取消: {streamer.Name}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"WebSocket录制过程中出错: {streamer.Name}");
                    }
                    finally
                    {
                        // 检查文件是否存在且有内容，如果有则进行封装
                        if (File.Exists(outputFilePath))
                        {
                            if (new FileInfo(outputFilePath).Length > 0)
                            {
                                _logger.LogInformation($"正在对文件进行FFmpeg封装: {outputFilePath}");
                                await EncapsulateWithFFmpeg(outputFilePath, streamer);
                            }
                            else
                            {
                                File.Delete(outputFilePath);
                                _logger.LogInformation("已删除空文件: {0}", outputFilePath);
                            }
                        }
                        // 从活跃录制列表中移除
                        lock (_recordingsLock)
                        {
                            _activeRecordings.Remove(cts);
                        }

                        // 触发录制完成事件
                        RecordingCompleted?.Invoke(this, new RecordingCompletedEventArgs(streamer));
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"开始WebSocket录制 {streamer.Name} 的直播时出错");
                return false;
            }
        }

        // 处理程序退出事件
        private void OnProcessExit(object sender, EventArgs e)
        {
            CleanupRecordings();
        }

        // 清理并关闭所有WebSocket连接
        private void CleanupRecordings()
        {
            try
            {
                _logger.LogInformation("程序正在退出，正在取消所有WebSocket录制...");

                // 复制列表以避免并发修改
                List<CancellationTokenSource> recordingsToCleanup;
                lock (_recordingsLock)
                {
                    recordingsToCleanup = new List<CancellationTokenSource>(_activeRecordings);
                }

                // 取消所有录制
                foreach (var cts in recordingsToCleanup)
                {
                    try
                    {
                        if (!cts.IsCancellationRequested)
                        {
                            cts.Cancel();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "取消WebSocket录制时出错");
                    }
                }

                // 等待一段时间让所有录制任务完成
                Thread.Sleep(1000);

                // 等待所有FFmpeg任务完成
                Task[] ffmpegTasksToWait;
                lock (_ffmpegTasksLock)
                {
                    ffmpegTasksToWait = _activeFFmpegTasks.ToArray();
                }

                if (ffmpegTasksToWait.Length > 0)
                {
                    _logger.LogInformation($"等待 {ffmpegTasksToWait.Length} 个FFmpeg任务完成...");
                    try
                    {
                        // 等待所有FFmpeg任务完成，但设置一个合理的超时时间（例如30秒）
                        bool allCompleted = Task.WaitAll(ffmpegTasksToWait, TimeSpan.FromSeconds(1800));
                        if (allCompleted)
                        {
                            _logger.LogInformation("所有FFmpeg任务已完成");
                        }
                        else
                        {
                            _logger.LogWarning("等待FFmpeg任务超时，可能有未完成的任务");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "等待FFmpeg任务完成时出错");
                    }
                }

                // 清空列表
                lock (_recordingsLock)
                {
                    _activeRecordings.Clear();
                }

                _logger.LogInformation("所有WebSocket录制已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理WebSocket录制时发生错误");
            }
        }


        /// <summary>
        /// 使用FFmpeg封装WebSocket录制的数据为标准MP4文件
        /// </summary>
        /// <param name="inputFilePath">输入文件路径</param>
        /// <param name="streamer">主播信息</param>
        /// <returns>处理任务</returns>
        private async Task EncapsulateWithFFmpeg(string inputFilePath, Streamer streamer)
        {
            Task ffmpegTask = null;
            try
            {
                _logger.LogInformation($"开始使用FFmpeg封装文件: {inputFilePath}");

                // 为输出文件创建临时路径
                string outputFilePath = Path.Combine(
                    Path.GetDirectoryName(inputFilePath),
                    Path.GetFileNameWithoutExtension(inputFilePath) + "_output.mp4");

                // 使用FFMpegCore构建命令
                var processor = FFMpegArguments
                    .FromFileInput(inputFilePath)
                    .OutputToFile(outputFilePath, true, options => options
                        .WithCopyCodec()
                        .ForceFormat("mp4"));

                // 创建一个任务来执行FFmpeg命令
                ffmpegTask = processor.ProcessAsynchronously();

                // 注册到全局集合
                lock (_ffmpegTasksLock)
                {
                    _activeFFmpegTasks.Add(ffmpegTask);
                }

                // 等待任务完成
                await ffmpegTask;

                if (File.Exists(outputFilePath))
                {
                    var outputInfo = new FileInfo(outputFilePath);
                    var sizeMB = outputInfo.Length / (1024.0 * 1024.0);

                    _logger.LogInformation(
                        "FFmpeg封装完成: {0}, 文件大小: {1:F2} MB, 输出路径: {2}",
                        streamer.Name, sizeMB, outputFilePath);

                    if (File.Exists(inputFilePath))
                    {
                        File.Delete(inputFilePath);
                        File.Move(outputFilePath, inputFilePath);
                        _logger.LogInformation("已用封装后的文件替换原始录制文件");
                    }
                }
                else
                {
                    _logger.LogError($"FFmpeg封装失败: {streamer.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"FFmpeg封装过程中出错: {streamer.Name}");
            }
            finally
            {
                // 从全局集合中移除任务
                if (ffmpegTask != null)
                {
                    lock (_ffmpegTasksLock)
                    {
                        _activeFFmpegTasks.Remove(ffmpegTask);
                    }
                }
            }
        }

    }
}
