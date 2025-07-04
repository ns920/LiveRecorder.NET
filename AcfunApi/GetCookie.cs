using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AcfunApi
{
    public static class GetCookie
    {
        public static List<KeyValuePair<string, string>> Get(HttpResponseMessage message)
        {
            List<KeyValuePair<string, string>> cookies = new();
            message.Headers.TryGetValues("Set-Cookie", out var setCookie);
            var setCookieString = setCookie.Single();
            var cookieTokens = setCookieString.Split(';');
            foreach (var cookieToken in cookieTokens)
            {
                var cookie = cookieToken.Split('=');
                cookies.Add(new KeyValuePair<string, string>(cookie[0], cookie[1]));
            }
            return cookies;
        }
    }
}