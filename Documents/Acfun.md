# General
由于Acfun存在官方录像，LiveRecorder并不使用直接录制的方式录制Acfun的直播。
在Acfun进行录制依靠两个模块：
- AcfunMain，负责将Acfun的全站直播信息实时存入本地数据库
- Acfun，对于Acfun的直播指出的直播间自动获取录播链接

配置时，用户需要先配置AcfunMain模块，再对需要的直播间配置Acfun。

Sample：
```json
{

    {
      "type": "acfunmain",
      "name": "acfunmain",
      "channel": "acfunmain"
    },
    {
      "type": "acfun",
      "name": "localname",
      "channel": "35764170"
    }

}
```
# Acfun.Database
提供数据库查询功能的桌面程序，对于没有配置Acfun模块的直播也可以用于获取下载链接。

# 下载录播
获取到的录播链接为m3u8直播流，推荐使用[yt-dlp](https://github.com/yt-dlp/yt-dlp)等工具下载录播。