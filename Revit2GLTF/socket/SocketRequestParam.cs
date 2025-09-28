using Fleck;

namespace Revit2Gltf.socket
{
    public class SocketRequestParam
    {
        /// <summary>
        /// 发起转换时后端协商好的请求来源平台id
        /// </summary>
        public string platformID { get; set; }
        /// <summary>
        /// 发起转换时后端传来的该次请求唯一id，完成后返回
        /// </summary>
        public string identityID { get; set; }

        /// <summary>
        /// 发起转换时后端传来的bim文件存储id
        /// </summary>
        public int fileId { get; set; }
        /// <summary>
        /// 登录用户 session
        /// </summary>
        public string sessionId { get; set; }
        /// <summary>
        /// rvt文件原路径
        /// </summary>
        public string filePath { get; set; }
        /// <summary>
        /// 转换配置
        /// </summary>
        public SocketTransformSetting options { get; set; }
        /// <summary>
        /// 请求的租户网络地址
        /// </summary>
        public string origin { get; set; }

        /// <summary>
        /// 对应的socket在allSockets对象池中的索引
        /// </summary>
        public int socketKey { get; set; }
        /// <summary>
        /// 对应的socket
        /// </summary>
        public IWebSocketConnection socket { get; set; }
    }
    public class PlatformList
    {
        public string BomFlow { get; set; }
        public string Manual { get; set; }
        public string Other { get; set; }
    }
}
