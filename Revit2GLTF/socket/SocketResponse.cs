namespace Revit2Gltf.socket
{
    internal class SocketResponse
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
        public int fileId {  get; set; }
        /// <summary>
        /// 登录用户 session
        /// </summary>
        public string sessionId { get; set; }
        /// <summary>
        /// 转换状态
        /// </summary>
        public string type { get; set; }
        /// <summary>
        /// 运行时间
        /// </summary>
        public double runSeconds { get; set; }
        /// <summary>
        /// 导出路径
        /// </summary>
        public string gltfPath { get; set; }
        /// <summary>
        /// rvt文件原路径
        /// </summary>
        public string filePath { get; set; }
        
        /// <summary>
        /// bom的json数据
        /// </summary>
        public string bomdata { get; set; }
        /// <summary>
        /// 场景树
        /// </summary>
        public string sceneTreeData { get; set; }
        
        /// <summary>
        /// 转换配置
        /// </summary>
        public SocketTransformSetting options { get; set; }
        /// <summary>
        /// 转换失败信息
        /// </summary>
        public string errMsg { get; set; }
        /// <summary>
        /// 租户请求地址
        /// </summary>
        public string origin { get; set; }
    }
}
