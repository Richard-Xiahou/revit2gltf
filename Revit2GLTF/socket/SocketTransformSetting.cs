using Newtonsoft.Json.Linq;
using System;
using System.Text;

namespace Revit2Gltf.socket
{
    public static class TransformSettingEnum{
        // 可选视图常量
        public const string Default = "Default";
        public const string Name = "Name";

        // 可选视觉样式常量
        public const string Colour = "Colour";
        public const string Realistic = "Realistic";
        public const string ViewDefault = "ViewDefault";

        // 可选坐标参考常量
        public const string Origin = "Origin";
        public const string ProjectBasePoint = "ProjectBasePoint";
        public const string MeasuringPoint = "MeasuringPoint";
    }

    public class SocketTransformSetting
    {
        /// <summary>
        /// 是否使用draco压缩
        /// </summary>
        public bool UseDraco { get; set; } = false;
        /// <summary>
        /// 生成的gltf模型最优化（网格合并/模型减面）
        /// 与 draco 互斥
        /// </summary>
        public bool Optimize { get; set; } = true;
        /// <summary>
        /// 是否导出属性
        /// </summary>
        public bool ExportProperty { get; set; } = true;
        /// <summary>
        /// 转换视图 - Default：默认3D视图 | Name：按传入名称查找视图
        /// </summary>
        public string View { get; set; } = "Default";
        /// <summary>
        /// 转换视图名称 - View = "Name"时输入有效
        /// </summary>
        public string ViewName { get; set; } = string.Empty;
        /// <summary>
        /// 转换视觉样式 - Colour:着色模式 | Realistic-真实模式 | ViewDefault-视图默认
        /// </summary>
        public string DisplayStyle { get; set; } = "Colour";
        /// <summary>
        /// 坐标参考 - Origin:默认原点 | ProjectBasePoint：项目基点 | MeasuringPoint：测量点
        /// </summary>
        public string CoordinateReference { get; set; } = "Origin";

        public SocketTransformSetting() { }

        public SocketTransformSetting(JToken v) {
            if(v["useDraco"] != null)
            {
                UseDraco = (bool)v["useDraco"];
            }

            if (v["exportProperty"] != null)
            {
                ExportProperty = (bool)v["exportProperty"];
            }

            if (v["optimize"] != null)
            {
                Optimize = (bool)v["optimize"];
            }

            if (v["view"] != null)
            {
                View = (string)v["view"];
            }

            if (v["viewName"] != null)
            {
                ViewName = v["viewName"].ToString();
            }

            if (v["displayStyle"] != null)
            {
                DisplayStyle = v["displayStyle"].ToString();
            }

            if (v["coordinateReference"] != null)
            {
                CoordinateReference = v["coordinateReference"].ToString();
            }

        }

        public override string ToString() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("{");
            stringBuilder.Append($"'UseDraco':{UseDraco}");
            stringBuilder.Append($",'Optimize':{Optimize}");
            stringBuilder.Append($",'ExportProperty':{ExportProperty}");
            stringBuilder.Append($",'View':'{View}'");
            stringBuilder.Append($",'ViewName':'{ViewName}'");
            stringBuilder.Append($",'DisplayStyle':'{DisplayStyle}'");
            stringBuilder.Append($",'CoordinateReference':'{CoordinateReference}'");
            stringBuilder.Append("}");
            return stringBuilder.ToString();
        }
    }
}