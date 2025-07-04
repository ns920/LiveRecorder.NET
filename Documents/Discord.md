# 录播机支持发送Discord开播提醒，但是需要自行配置Discord Bot。

1. 用户需要在Discord中创建一个Bot，并获取Bot的Token。[Discord Developers](https://discord.com/developers/applications)
1. 从页面中的OAuth2选项卡中，获取Bot的邀请链接，打开链接并邀请Bot到你的服务器。
1. 打开Discord的开发者模式，右键自己的信息，复制用户ID。
1. 将得到的用户ID和Bot的Token填入配置文件中。：
```json
"accounts": {
    "discord_token": "",
    "discord_userid": ""
  }
```
1.配置后，Bot会在开播时私聊你。