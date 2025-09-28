using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf.glTF
{
    internal class glTFSetting
    {
        // 是否使用Draco压缩
        public bool useDraco { get; set; } = false;

        // 转换后的文件存储路径和名称
        public string fileName { get; set; }

        // 导出Revit属性集
        public bool exportProperty { get; set; } = false;
    }
}
