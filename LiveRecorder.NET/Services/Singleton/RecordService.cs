using DotNetTools.SharpGrabber;
using DotNetTools.SharpGrabber.Converter;
using DotNetTools.SharpGrabber.Grabbed;
using FFMpegCore;
using LiveRecorder.NET.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Services.Singleton
{
    class RecordService
    {
        private readonly ILogger<RecordService> _logger;
        private readonly IConfiguration _configuration;
        private readonly List<Process> _activeProcesses = new List<Process>();
        private readonly object _processLock = new object();
        private readonly IHttpClientFactory _httpClientFactory;
        public RecordService(ILogger<RecordService> logger, IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            // 注册应用程序退出事件
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        // 定义录制完成事件
        public event EventHandler<RecordingCompletedEventArgs> RecordingCompleted;

        // 定义进程输出事件
        public event EventHandler<ProcessOutputEventArgs> ProcessOutputReceived;

        // 创建事件参数类
        public class RecordingCompletedEventArgs : EventArgs
        {
            public Streamer Streamer { get; }

            public RecordingCompletedEventArgs(Streamer streamer)
            {
                Streamer = streamer;
            }
        }

        // 进程输出事件参数类
        public class ProcessOutputEventArgs : EventArgs
        {
            public Streamer Streamer { get; }
            public string Data { get; }
            public bool IsError { get; }

            public ProcessOutputEventArgs(Streamer streamer, string data, bool isError)
            {
                Streamer = streamer;
                Data = data;
                IsError = isError;
            }
        }

        /// <summary>
        /// 开始录制直播流
        /// </summary>
        /// <param name="url">m3u8直播流地址</param>
        /// <param name="streamer">主播信息</param>
        /// <returns>是否成功开始录制</returns>
        public bool StartRecording(string url, Streamer streamer)
        {
            try
            {
                if (string.IsNullOrEmpty(url))
                {
                    _logger.LogError("直播流地址为空，无法开始录制");
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

                var proxy = _configuration["proxy"];
                // 使用FFMpegCore开始录制
                // 异步启动以避免阻塞当前线程
                Task.Run(async () =>
                {
                    try
                    {
                        var grabber = GrabberBuilder.New()
                            .UseDefaultServices()
                            .AddHls()
                            .Build();
                        var result = await grabber.GrabAsync(new Uri(url));

                        var metadataResources = result.Resources<GrabbedHlsStreamMetadata>().ToArray();
                        if (metadataResources.Length > 0)
                        {
                            // Description for one or more M3U8 playlists
                            GrabbedHlsStreamMetadata selection;
                            if (metadataResources.Length == 1)
                            {
                                selection = metadataResources.Single();
                            }
                            else
                            {
                                _logger.LogInformation("=== Streams ===");
                                for (var i = 0; i < metadataResources.Length; i++)
                                {
                                    var res = metadataResources[i];
                                    _logger.LogInformation("{0}. {1}", i + 1, $"{res.Name} {res.Resolution}");
                                }
                                selection = metadataResources[0];
                            }

                            // Get information from the HLS stream
                            var grabbedStream = await selection.Stream.Value;
                            await Grab(grabbedStream, selection, result, streamer.CustomHeader);
                            return;
                        }

                    }


                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"录制过程中出错: {streamer.Name}");
                    }
                    finally
                    {
                        // 无论如何都触发完成事件
                        RecordingCompleted?.Invoke(this, new RecordingCompletedEventArgs(streamer));
                    }
                });

                _logger.LogInformation($"已开始录制 {streamer.Name} 的直播");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"开始录制 {streamer.Name} 的直播时出错");
                return false;
            }
        }

        // 处理程序退出事件
        private void OnProcessExit(object sender, EventArgs e)
        {

        }
        private async Task Grab(GrabbedHlsStream stream, GrabbedHlsStreamMetadata metadata, GrabResult grabResult, Dictionary<string, string> headers)
        {
            _logger.LogInformation("=== Downloading ===");
            _logger.LogInformation("{0} segments", stream.Segments.Count);
            _logger.LogInformation("Duration: {0}", stream.Length);

            var tempFiles = new List<string>();
            try
            {
                for (var i = 0; i < stream.Segments.Count; i++)
                {
                    var segment = stream.Segments[i];
                    Console.Write($"Downloading segment #{i + 1} {segment.Title}...");
                    var outputPath = Path.GetTempFileName();
                    tempFiles.Add(outputPath);
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                    client.DefaultRequestHeaders.Add("Referer", "https://twitcasting.tv/");
                    client.DefaultRequestHeaders.Add("Host", "twitcasting.tv");
                    if (headers is not null && headers.Count > 0)
                    {
                        foreach (var header in headers)
                        {
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);
                        }
                    }
                    using var responseStream = await client.GetStreamAsync(segment.Uri);
                    using var inputStream = await grabResult.WrapStreamAsync(responseStream);
                    using var outputStream = new FileStream(outputPath, FileMode.Create);
                    await inputStream.CopyToAsync(outputStream);
                }

                CreateOutputFile(tempFiles, metadata);
            }
            finally
            {
                foreach (var tempFile in tempFiles)
                {
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
                Console.WriteLine("Cleaned up temp files.");
            }
        }

        private static void CreateOutputFile(List<string> tempFiles, GrabbedHlsStreamMetadata metadata)
        {
            Console.WriteLine("All segments were downloaded successfully.");
            Console.Write("Enter a path for the output file: ");
            var outputPath = Console.ReadLine();
            var concatenator = new MediaConcatenator(outputPath)
            {
                OutputMimeType = metadata.OutputFormat.Mime,
                OutputExtension = metadata.OutputFormat.Extension,
            };
            foreach (var tempFile in tempFiles)
                concatenator.AddSource(tempFile);
            concatenator.Build();
            Console.WriteLine("Output file created successfully!");
        }
    }
}
