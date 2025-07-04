using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiveRecorder.NET.Models.Acfun
{
    public class OriginalLiveData
    {
        public Channellistdata? channelListData { get; set; }
        public bool? isError { get; set; }
        public int? totalCount { get; set; }
        public Channeldata? channelData { get; set; }
        public Livelist[]? liveList { get; set; }
        public object[]? recommendAuthorsData { get; set; }
        public Channelfilters? channelFilters { get; set; }
    }

    public class Channellistdata
    {
        public int? result { get; set; }
        public string? requestId { get; set; }
        public Livelist[]? liveList { get; set; }
        public int? count { get; set; }
        public string? pcursor { get; set; }
        public string? hostname { get; set; }
        public int? totalCount { get; set; }
    }

    public class Livelist
    {
        public bool? disableDanmakuShow { get; set; }
        public string? requestId { get; set; }
        public string? groupId { get; set; }
        public int? action { get; set; }
        public string? href { get; set; }
        public Type? type { get; set; }
        public string? streamName { get; set; }
        public User? user { get; set; }
        public int? authorId { get; set; }
        public int? likeCount { get; set; }
        public int? onlineCount { get; set; }
        public string[]? coverUrls { get; set; }
        public long? createTime { get; set; }
        public string? liveId { get; set; }
        public string? title { get; set; }
        public bool? portrait { get; set; }
        public string? formatLikeCount { get; set; }
        public string? formatOnlineCount { get; set; }
        public bool? panoramic { get; set; }
        public string? bizCustomData { get; set; }
        public int? cdnAuthBiz { get; set; }
        public bool? hasFansClub { get; set; }
        public bool? paidShowUserBuyStatus { get; set; }
    }

    public class Type
    {
        public int? id { get; set; }
        public string? name { get; set; }
        public int? categoryId { get; set; }
        public string? categoryName { get; set; }
    }

    public class User
    {
        public int? action { get; set; }
        public string? href { get; set; }
        public string? name { get; set; }
        public string? signature { get; set; }
        public int? avatarFrame { get; set; }
        public int? sexTrend { get; set; }
        public int[]? verifiedTypes { get; set; }
        public int? nameColor { get; set; }
        public bool? isFollowing { get; set; }
        public string? contributeCount { get; set; }
        public string? avatarFramePcImg { get; set; }
        public string? avatarFrameMobileImg { get; set; }
        public int? followingStatus { get; set; }
        public int? fanCountValue { get; set; }
        public int? verifiedType { get; set; }
        public string? verifiedText { get; set; }
        public int? gender { get; set; }
        public string? followingCount { get; set; }
        public string? headUrl { get; set; }
        public string? liveId { get; set; }
        public Socialmedal? socialMedal { get; set; }
        public Headcdnurl[]? headCdnUrls { get; set; }
        public int? followingCountValue { get; set; }
        public int? contributeCountValue { get; set; }
        public string? fanCount { get; set; }
        public string? avatarImage { get; set; }
        public Userheadimginfo? userHeadImgInfo { get; set; }
        public bool? isFollowed { get; set; }
        public string? id { get; set; }
        public bool? isJoinUpCollege { get; set; }
        public string? comeFrom { get; set; }
    }

    public class Socialmedal
    {
    }

    public class Userheadimginfo
    {
        public int? width { get; set; }
        public int? height { get; set; }
        public int? size { get; set; }
        public int? type { get; set; }
        public bool? animated { get; set; }
        public Thumbnailimage? thumbnailImage { get; set; }
        public string? thumbnailImageCdnUrl { get; set; }
    }

    public class Thumbnailimage
    {
        public Cdnurl[]? cdnUrls { get; set; }
    }

    public class Cdnurl
    {
        public string? url { get; set; }
        public bool? freeTrafficCdn { get; set; }
    }

    public class Headcdnurl
    {
        public string? url { get; set; }
        public bool? freeTrafficCdn { get; set; }
    }

    public class Channeldata
    {
    }

    public class Channelfilters
    {
        public Livechanneldisplayfilter[]? liveChannelDisplayFilters { get; set; }
    }

    public class Livechanneldisplayfilter
    {
        public Displayfilter[]? displayFilters { get; set; }
    }

    public class Displayfilter
    {
        public int? filterType { get; set; }
        public int? filterId { get; set; }
        public string? name { get; set; }
        public string? cover { get; set; }
    }
}
