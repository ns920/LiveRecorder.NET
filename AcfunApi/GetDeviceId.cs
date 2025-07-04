using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AcfunApi
{
    public static class GetDeviceId
    {
        public static async Task<string> GetAsync()
        {
            using HttpClient httpClient = new HttpClient();
            try
            {

                var response = await httpClient.GetAsync("https://live.acfun.cn/");

                return GetCookie.Get(response).Where(x => x.Key == "_did").First().Value;
            }
            catch (Exception ex)
            {
                throw;
            }

        }
    }
}