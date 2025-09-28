using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Revit2Gltf.glTF;
using Revit2Gltf.socket;
using System;
using System.Collections.Generic;

namespace Revit2Gltf.utils
{
    // 定义 SceneNodeArray;最终保存的 nodes 每个子节点都是 SceneNodeArray 格式
    public class SceneNodeArray
    {
        public string name { get; set; }
        public string nodeId { get; set; }
        public List<SceneNodeArray> Children { get; set; }

        public SceneNodeArray()
        {
            Children = new List<SceneNodeArray>();
        }
    }

   /*
   // 已经在 revit2Gltf.glTF 中定义
    public class GLTF
    {
        public List<Scene> scenes { get; set; }
        public List<Node> nodes { get; set; }
        // Additional properties as needed
    }
    */
    // 顶级元素 Gltf.scenes 
    public class Scene
    {
        public int[] nodes { get; set; }
        public string name { get; set; }
    }

    // 顶级元素 Gltf.nodes
    public class Node
    {
        public string name { get; set; }
        public int[] children { get; set; } 
        // 其他属性
    }

    public class SceneTree
    {
        private GLTF _glTF;
        private List<SceneNodeArray> nodes = new List<SceneNodeArray>();

        internal SceneTree(GLTF glTF)
        {
            _glTF = glTF;
            Init();
        }

        private void Init()
        {
            foreach (var scene in _glTF.scenes)
            {
                foreach (int idx in scene.nodes)
                {
                    var obj = _glTF.nodes[idx];
                    if (obj != null)
                    {
                        Guid myUuid = Guid.NewGuid();
                        var parentNode = new SceneNodeArray(); 
                        if (obj.children != null && obj.children.Count > 0)
                        {
                            ParseChildren(parentNode.Children, obj.children);
                        }
                        parentNode.name = obj.name;
                        parentNode.nodeId = myUuid.ToString();
                        nodes.Add(parentNode);
                    }
                }
            }
        }

        private void ParseChildren(List<SceneNodeArray> parentList, List<int> childrenArray)
        {
            foreach (int idx in childrenArray)
            {
                var obj = _glTF.nodes[idx];
                if (obj != null)
                {
                    Guid myUuid = Guid.NewGuid();
                    var childNode = new SceneNodeArray
                    {
                        name = obj.name,
                        nodeId = myUuid.ToString(),
                        Children = new List<SceneNodeArray>()
                    };
                    if (obj.children != null && obj.children.Count > 0)
                    {
                        ParseChildren(childNode.Children, obj.children);
                    }
                    parentList.Add(childNode);
                }
            }
        }

        public string ToJsonStr()
        {
            return JsonConvert.SerializeObject(nodes, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public List<SceneNodeArray> GetNodes()
        {
            return nodes;
        }
    }
}