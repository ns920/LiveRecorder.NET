using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;

namespace AcfunApi
{
    public class AcfunApiRequest
    {
        public LoginInformation _loginInformation { get; set; }

        public AcfunApiRequest()
        {
            _loginInformation = GetAcfunToken.Get().Result;
        }
        public AcfunApiRequest(LoginInformation loginInformation)
        {
            _loginInformation = loginInformation;
        }
        public async static Task<LoginInformation> GetLoginInformation()
        {
            return await GetAcfunToken.Get();
        }

        public async Task<string> Post(string url, List<KeyValuePair<string, string>> formParams)
        {
            using HttpClient httpClient = new HttpClient();
            using (var request = new HttpRequestMessage())
            {
                Uri uri = new Uri($"{url}&userId={_loginInformation.userId}&did={_loginInformation._did}&{_loginInformation.serviceTokenName}={_loginInformation.serviceToken}");
                request.Method = new HttpMethod("POST");
                request.RequestUri = uri;
                var sign = new KeyValuePair<string, string>("__clientSign", GetSign.Sign(uri, formParams, _loginInformation.securityKey));
                formParams.Add(sign);
                request.Content = new FormUrlEncodedContent(formParams);
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                httpClient.DefaultRequestHeaders.Add("Cookie", $"_did={_loginInformation._did};");
                httpClient.DefaultRequestHeaders.Add("Referer", "https://live.acfun.cn/");
                var response = await httpClient.SendAsync(request);
                return await response.Content.ReadAsStringAsync();
            }
        }

        public async Task<string> GetNoSign(string url)
        {
            string urlWithParams = $"{url}&userId={_loginInformation.userId}&did={_loginInformation._did}&{_loginInformation.serviceTokenName}={_loginInformation.serviceToken}";
            return await GetNoSignNoLogin(urlWithParams);
        }

        public async Task<string> GetNoSignNoLogin(string url)
        {
            Uri uri = new Uri(url);
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("cookie", $"_did={_loginInformation._did};");
            var response = await httpClient.GetAsync(uri);
            return await response.Content.ReadAsStringAsync();
        }
    }
}