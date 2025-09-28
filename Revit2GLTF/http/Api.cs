using Newtonsoft.Json.Linq;
using Revit2Gltf.utils;
using System;
using System.Collections.Generic;
using Fleck;
using System.Diagnostics;

namespace Revit2Gltf.http
{
    public class Response<T>
    {
        public int code { get; set; }
        public string message { get; set; }
        public T data { get; set; }

        public Response(int code, string msg, T data)
        {
            this.code = code;
            this.message = msg;
            this.data = data;
        }
    }

    public class OssSignRes
    {
        public string accessid { get; set; }
        public string host { get; set; }
        public string key { get; set; }
        public string policy { get; set; }
        public string signature { get; set; }
        public string url { get; set; }
    }

    public class Api
    {
        #region oss云存储 
        /// <summary>
        /// 获取签名
        /// </summary>
        /// <param name="filePath"></param>
        public static OssSignRes GetOssSign(string filePath)
        {
            string res = Request.Get($"/inner/getUploadSignature?path={filePath}");
            // 序列化ren为json对象
            Response<OssSignRes> json = Newtonsoft.Json.JsonConvert.DeserializeObject<Response<OssSignRes>>(res);
            return json.data;
        }

        /// <summary>
        /// 上传入口
        /// </summary>
        /// 
        public static OssSignRes UploadOss(string path, string ossSavePath, string fileName)
        {
            // 获取签名
            OssSignRes ossSign = GetOssSign($"{ossSavePath}/{fileName}");

            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                { "key", ossSign.key },
                { "policy", ossSign.policy },
                { "OSSAccessKeyId", ossSign.accessid },
                { "success_action_status", "200" },
                { "signature", ossSign.signature }
            };

            Log.Debug($"[Progress]  已获取OSS签名! key:{ossSign.key} host:{ossSign.host}");
            Log.Debug($"[Progress]  开始上传...");

            Stopwatch stopWatch = new Stopwatch();
            //测量运行时间
            stopWatch.Start();
            // 开始上传
            Request.UploadFileToOss(ossSign.host,path, dic);
            // 停止计时
            stopWatch.Stop();

            Log.Debug($"[Progress]  上传完毕！用时 {stopWatch.Elapsed.TotalSeconds} 秒.");

            return ossSign;
        }
        #endregion

        public static Response<object> SaveStructureData(int bimFileId, List<NodeArray> nodes)
        {
            // nodes json string
            string jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(nodes);
            string res = Request.Post($"/cover/schema/{bimFileId}?modelType=RVT", jsonStr);

            return Newtonsoft.Json.JsonConvert.DeserializeObject<Response<object>>(res); ;
        }

        public static Response<object> DelStructureData(int bimFileId, IWebSocketConnection socket)
        {
            string res = Request.Delete($"/cover/schema/{bimFileId}?modelType=RVT");
  
            return Newtonsoft.Json.JsonConvert.DeserializeObject<Response<object>>(res);
        }
    }
}