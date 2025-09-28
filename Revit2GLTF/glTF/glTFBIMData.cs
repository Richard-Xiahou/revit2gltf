using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Revit2Gltf.glTF
{
    internal class glTFBIMData
    {
        /// <summary>
        /// ElementID
        /// </summary>
        public int elementID {  get; set; }

        /// <summary>
        /// UniqueId
        /// </summary>
        public string uniqueId { get; set; }

        /// <summary>
        /// 构件属性
        /// </summary>
        public List<glTFParameterGroup> parameters { get; set; }
    }
}
