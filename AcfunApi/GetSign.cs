using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NaturalSort.Extension;
using System.IO;
using System.Net.Http.Headers;

using System.Text;

using Microsoft.IdentityModel.Tokens;

namespace AcfunApi
{
    public static class GetSign
    {
        public static string Sign(Uri requestUri, List<KeyValuePair<string, string>> formParams, string securitykey)
        {
            List<KeyValuePair<string, string>> queryParams = requestUri.Query.Substring(1).Split('&').Select(x =>
            {
                string[] strings = x.Split('=');
                return new KeyValuePair<string, string>(strings[0], strings[1]);
            }).ToList();
            List<KeyValuePair<string, string>> allParams = queryParams.Concat(formParams).ToList();
            var allParamsSorted = allParams.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase.WithNaturalSort());
            List<string> paramStr = allParamsSorted.Select(x => $"{x.Key}={x.Value}").ToList();
            string querystring = "";
            foreach (var param in paramStr)
            {
                querystring += $"{param}&";
            }
            querystring = querystring.Substring(0, querystring.Length - 1);
            var minute = GetTimeStamp() / 60;
            var random = (Int64)new Random().Next();
            var nonce = minute | (random << 32);
            string noncestr = nonce.ToString();
            string needSigned = "POST&" + requestUri.LocalPath + "&" + querystring + "&" + noncestr;
            var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
            {
                writer.Write(nonce);
            }
            var bytes = stream.ToArray().Reverse().ToArray();
            bytes = bytes.Concat(HmacSHA256.encryptToBase64(needSigned, securitykey)).ToArray();
            string signature = Base64UrlEncoder.Encode(bytes);
            return signature;
        }

        private static long GetTimeStamp()
        {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000000;
        }
    }
}