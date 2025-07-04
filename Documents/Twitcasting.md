# General
Twitcasting录播模块提供完整的录播下载功能，对于需要权限的直播需要用户自行填写相关参数，公开直播可以直接下载。

## 会员直播
会员直播需要用户身份才能下载，请使用浏览器登录Twitcasting，获取cookie后填写到配置文件中。
```json
  "accounts": {
    "twitcasting_username": "username",
    "twitcasting_token": "1HWwefaefaefaefaAX-Uefae63UY",
    }
```

## 密码直播
密码直播需要用户填写密码。
```json
  {
      "type": "twitcasting",
      "name": "test",
      "channel": "c:test",
      "livepassword": "合言葉"
    }
```