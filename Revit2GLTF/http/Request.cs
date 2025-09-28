using Revit2Gltf.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf.http
{
    /// <summary>
    /// http协议访问方法封装
    /// </summary>
    public static class  UrlConfig
    {
        #region socket 
         public static string ScoketIp = "0.0.0.0";

        public static int ScoketPort = 8081;
        #endregion

        #region ��˽ӿ�
        public static string BaseUrl = "http://192.168.5.113:9005/czy-bom";

        public static void SetBaseUrl(string origin)
        {
            // 判断origin 是否包含http
            if (!origin.Contains("http"))
            {
                origin = $"http://{origin}";
            }
            // 接口请求，https换为http
            if(origin.Contains("https"))
            {
                string u = origin.Replace("https://","");
                origin = $"http://{u}";
            }
            BaseUrl = $"{origin}/czy-bom";
            Request.BaseUrl = BaseUrl;
        }
        #endregion
    }

    /// <summary>
    /// http协议访问方法封装
    /// </summary>
    public class Request
    {
        public static string BaseUrl = UrlConfig.BaseUrl;

        /// <summary>
        /// 上传文件到OSS
        /// </summary>
        public static async Task UploadFileToOssAsync(string path, OssSignRes ossConfig)
        {
            var fileName = Path.GetFileName(path);
            var multipartForm = new MultipartFormDataContent($"-----{Guid.NewGuid().ToString()}");
            // 读取文件
            Stream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var streamContent = new StreamContent(fileStream);
            multipartForm.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(ossConfig.accessid)), "\"OSSAccessKeyId\"");
            multipartForm.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(ossConfig.key)), "\"key\"");
            multipartForm.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(ossConfig.policy)), "\"policy\"");
            multipartForm.Add(new ByteArrayContent(Encoding.UTF8.GetBytes(ossConfig.signature)), "\"signature\"");
            multipartForm.Add(streamContent, "\"file\"", $"\"{fileName}\"");

            // 修饰boundary，移除双引号
            var boundary = multipartForm.Headers.ContentType.Parameters.First(o => o.Name == "boundary");
            boundary.Value = boundary.Value.Replace("\"", String.Empty);

            //修饰文件表单域，移除FileNameStar
            streamContent.Headers.ContentDisposition.FileNameStar = null;
            HttpClient client = new HttpClient();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var rsp = await client.PostAsync(ossConfig.host, multipartForm);
            var content = await rsp.Content.ReadAsStringAsync();
        }

        /// <summary>
        /// 请求上传到阿里云
        /// </summary>
        /// <param name="url">上传地址</param>
        /// <param name="filepath">本地文件路径</param>
        /// <param name="dic">上传的数据信息</param>
        /// <returns></returns>
        public static string UploadFileToOss(string url, string filepath, Dictionary<string, string> dic)
        {
            try
            {
                string boundary = DateTime.Now.Ticks.ToString("X");

                byte[] boundarybytes = System.Text.Encoding.UTF8.GetBytes("--" + boundary + "\r\n");

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.CookieContainer = CookieHelper.GetCurrentCookieContainer();
                request.Timeout = 10 * 60 * 1000;
                // 解决一直超时的错误
                // request.ServicePoint.Expect100Continue = false;
                //request.KeepAlive = false;
                request.Method = "POST";
                request.ContentType = "multipart/form-data; boundary=" + boundary;

                Stream rs = request.GetRequestStream();

                var endBoundaryBytes = System.Text.Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");

                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n" + "\r\n" + "{1}" + "\r\n";
                if (dic != null)
                {
                    foreach (string key in dic.Keys)
                    {
                        rs.Write(boundarybytes, 0, boundarybytes.Length);

                        string formitem = string.Format(formdataTemplate, key, dic[key]);

                        byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);

                        rs.Write(formitembytes, 0, formitembytes.Length);
                    }
                }

                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n\r\n";
                {
                    rs.Write(boundarybytes, 0, boundarybytes.Length);

                    var header = string.Format(headerTemplate, "file", Path.GetFileName(filepath));

                    var headerbytes = System.Text.Encoding.UTF8.GetBytes(header);

                    rs.Write(headerbytes, 0, headerbytes.Length);

                    using (var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
                    {
                        var buffer = new byte[1024];

                        var bytesRead = 0;

                        while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            rs.Write(buffer, 0, bytesRead);
                        }
                    }
                    var cr = Encoding.UTF8.GetBytes("\r\n");

                    rs.Write(cr, 0, cr.Length);
                }

                rs.Write(endBoundaryBytes, 0, endBoundaryBytes.Length);

                var response = request.GetResponse() as HttpWebResponse;

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return "成功";
                }
            }
            catch (WebException ex)
            {
                string result = string.Empty;
                //响应流
                var mResponse = ex.Response as HttpWebResponse;
                if(mResponse != null)
                {
                    var responseStream = mResponse.GetResponseStream();
                    if (responseStream != null)
                    {
                        var streamReader = new StreamReader(responseStream, Encoding.UTF8);
                        //获取返回的信息
                        result = streamReader.ReadToEnd();
                        streamReader.Close();
                        responseStream.Close();
                    }
                    return "获取数据失败，请重试！" + ex.ToString() + "  返回数据" + result;
                }
                else
                {
                    throw new Exception($"转换成功，上传至OSS失败，status:${ex.Status},原因：{ex.Message}");
                }
            }
            return "失败";
        }

        /// <summary>
        /// 指定Url地址使用Get 方式获取全部字符串
        /// </summary>
        /// <param name="url">请求链接地址</param>
        /// <returns></returns>
        public static string Get(string url)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(BaseUrl + url);
            req.CookieContainer = CookieHelper.GetCurrentCookieContainer();
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            try
            {
                //获取内容
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            finally
            {
                stream.Close();
            }
            return result;
        }

        /// <summary>
        /// 发送Get请求
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="dic">请求参数定义</param>
        /// <returns></returns>
        public static string Get(string url, Dictionary<string, string> dic)
        {
            string result = "";
            StringBuilder builder = new StringBuilder();
            builder.Append(BaseUrl + url);
            if (dic.Count > 0)
            {
                builder.Append("?");
                int i = 0;
                foreach (var item in dic)
                {
                    if (i > 0)
                        builder.Append("&");
                    builder.AppendFormat("{0}={1}", item.Key, item.Value);
                    i++;
                }
            }
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(builder.ToString());
            req.CookieContainer = CookieHelper.GetCurrentCookieContainer();
            //添加参数
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            try
            {
                //获取内容
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
            }
            finally
            {
                stream.Close();
            }
            return result;
        }

        /// <summary>
        ///  发送DELETE请求，返回状态：200成功，201失败
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string Delete(string url)
        {
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(BaseUrl + url);
            myRequest.CookieContainer = CookieHelper.GetCurrentCookieContainer();
            myRequest.Method = "DELETE";
            // 获得接口返回值
            HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse();
            StreamReader reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8);
            string result = reader.ReadToEnd();
            reader.Close();
            resp.Close();
            return result;
        }


        #region Post提交
        /// <summary>
        /// 指定地址使用Post方式获取
        /// </summary>
        /// <param name="url">请求后台地址</param>
        /// <returns></returns>
        public static string Post(string url)
        {
            string result = "";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(BaseUrl + url);
            req.CookieContainer = CookieHelper.GetCurrentCookieContainer();
            req.Method = "POST";
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            //获取内容
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }

        /// <summary>
        /// 发送一个POST请求 
        /// </summary>
        /// <param name="url">请求的地址</param>
        /// <param name="jsonStr">json字符串</param>
        /// <returns></returns>
        public static string Post(string url, string jsonStr)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BaseUrl + url);
            request.CookieContainer = CookieHelper.GetCurrentCookieContainer();
            request.Method = "POST";
            request.KeepAlive = false;
            request.AllowAutoRedirect = true;
            request.Accept = "application/json";
            request.ContentType = "application/json";
            using (StreamWriter dataStream = new StreamWriter(request.GetRequestStream()))
            {
                dataStream.Write(jsonStr);
            }
            //  获取服务器返回的响应体
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            string encoding = response.ContentEncoding;
            if (encoding == null || encoding.Length < 1)
            {
                encoding = "UTF-8";  //默认编码  
            }
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.GetEncoding(encoding));
            string retString = reader.ReadToEnd();
            return retString;
        }
        #endregion
    }
}

