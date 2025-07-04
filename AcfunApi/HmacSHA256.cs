using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AcfunApi
{
    public static class HmacSHA256
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="secret">待加密字符串</param>
        /// <param name="signKey">密钥</param>
        /// <returns></returns>
        public static string encrypt(string secret, string signKey)
        {
            string signRet = string.Empty;
            using (HMACSHA256 mac = new HMACSHA256(Convert.FromBase64String(signKey)))
            {
                byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(secret));
                signRet = ToHexString(hash);
            }
            return signRet.ToUpper();
        }

        public static byte[] encryptToBase64(string secret, string signKey)
        {
            using (HMACSHA256 mac = new HMACSHA256(Convert.FromBase64String(signKey)))
            {
                byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(secret));
                return hash;
            }
        }

        public static string ToHexString(byte[] bytes)
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                foreach (byte b in bytes)
                {
                    strB.AppendFormat("{0:x2}", b);
                }
                hexString = strB.ToString();
            }
            return hexString;
        }
    }
}