using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Web;
using System.Windows.Input;

namespace Revit2Gltf.utils
{
    /// <summary>
    /// Cookie������
    /// </summary>
    public class CookieHelper
    {
        private static Dictionary<string, string> CookieMap = new Dictionary<string, string>();

        /// <summary>
        /// ���һ��Cookie��24Сʱ���ڣ�
        /// </summary>
        /// <param name="cookiename"></param>
        /// <param name="cookievalue"></param>
        public static void SetCookie(string key, string value)
        {
            CookieMap.Add(key, value);
        }

        /// <summary>
        /// ��ȡָ��Cookie
        /// </summary>
        /// <param name="cookiename">cookiename</param>
        /// <returns></returns>
        public static string GetCookie(string key)
        {
            if (CookieMap.ContainsKey(key))
            {
                return CookieMap[key];
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// ��ȡ��ǰCookieContainer
        /// </summary>
        /// <param name="cookiename">cookiename</param>
        /// <returns></returns>
        public static CookieContainer GetCurrentCookieContainer()
        {
            string key = $"[{MessageStation.CurrentWsMessage.Value.origin}]{MessageStation.CurrentWsMessage.Value.fileId}";

            string httpCookie = GetCookie(key);

            CookieContainer cookieContainer = new CookieContainer();

            var netCookie = new Cookie("tenant_token_cookie", httpCookie, "/", ".czy3d.com");
          
            cookieContainer.Add(netCookie);
            return cookieContainer;
        }

        /// <summary>
        /// ���ָ��Cookie
        /// </summary>
        /// <param name="cookiename">cookiename</param>
        public static void ClearCookie(string key)
        {
            if (CookieMap.ContainsKey(key))
            {
                CookieMap.Remove(key);
            }
        }

        /// <summary>
        /// �����ǰCookie
        /// </summary>
        /// <param name="cookiename">cookiename</param>
        public static void ClearCurrentCookie()
        {
            string key = $"[{MessageStation.CurrentWsMessage.Value.origin}]{MessageStation.CurrentWsMessage.Value.fileId}";

            ClearCookie(key);
        }

        /// <summary>
        /// ���cookie
        /// </summary>
        public static void Clear() {
            foreach (KeyValuePair<string, string> kvp in CookieMap)
            {
                CookieMap.Remove(kvp.Key);
            }
        }
    }
}