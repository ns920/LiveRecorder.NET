using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Threading.Tasks;

namespace AcfunApi
{
    public static class GetAcfunToken
    {
        public static async Task<LoginInformation> Get()
        {
            var _did = await GetDeviceId.GetAsync();
            //先默认使用游客模式
            using HttpClient httpClient = new HttpClient();
            var content = new FormUrlEncodedContent(new KeyValuePair<string, string>[] { new KeyValuePair<string, string>("sid", "acfun.api.visitor") });
            httpClient.DefaultRequestHeaders.Add("cookie", $"_did={_did}");
            var response = await httpClient.PostAsync("https://id.app.acfun.cn/rest/app/visitor/login", content);
            var responseObject = JsonConvert.DeserializeObject<JObject>(await response.Content.ReadAsStringAsync());
            LoginInformation loginInformation = new LoginInformation()
            {
                _did = _did,
                securityKey = responseObject["acSecurity"].ToString(),
                serviceToken = responseObject["acfun.api.visitor_st"].ToString(),
                userId = long.Parse(responseObject["userId"].ToString()),
                serviceTokenName = "acfun.api.visitor_st"
            };
            return loginInformation;
        }
    }
}