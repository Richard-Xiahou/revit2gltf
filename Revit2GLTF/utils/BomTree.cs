using Autodesk.Revit.DB;
using Newtonsoft.Json;
using Revit2Gltf.glTF;
using Revit2Gltf.socket;
using System.Collections.Generic;

namespace Revit2Gltf.utils
{
    // 族类型
    public class FamilyTypeGroup
    {
        public string Name { get; set; }
        public List<string> Children { get; set; }
    }

    // 族
    public class FamilyGroup
    {
        public string Name { get; set; }
        public List<FamilyTypeGroup> Children { get; set; }
    }

    // 分类
    public class CategoryGroup
    {
        public string Name { get; set; }
        public List<FamilyGroup> Children { get; set; }
    }

    // 标高
    public class FloorGroup
    {
        public string Name { get; set; }
        public List<CategoryGroup> Children { get; set; }
    }

    // 根节点
    public class RootGroup
    {
        public string Name { get; set; } = "全部构件";
        public List<FloorGroup> Children { get; set; }
    }

    // 节点数组
    public class NodeArray
    {
        // File id
        public int file_id { get; set; }

        // Element name: floor + category + family + familyType,split by '-'
        public string name { get; set; }

        // Element id
        public int element_id { get; set; }

        // Element Category
        public string category { get; set; }

        // Element type: wall, floor, etc.
        public string type { get; set; }

        // Floor name
        public string floor { get; set; }

        // Attribute
        public List<glTFParameterGroup> attributes { get; set; }
    }

    public class BomTree
    {
        public Dictionary<string, RootGroup> tree = new Dictionary<string, RootGroup>();

        private GLTF _glTF;

        private Dictionary<string, glTFNode> floorMap = new Dictionary<string, glTFNode>();
        private Dictionary<string, glTFNode> categoryMap = new Dictionary<string, glTFNode>();
        private Dictionary<string, glTFNode> familyMap = new Dictionary<string, glTFNode>();
        private Dictionary<string, glTFNode> familyTypeMap = new Dictionary<string, glTFNode>();
        private List<NodeArray> nodes = new List<NodeArray>();

        internal BomTree(GLTF glTF) {
            _glTF = glTF;

            // 初始化树结构
            tree.Add("root", new RootGroup {
                Children = new List<FloorGroup> {}
            });
        }

        // IExportContext 添加mesh前调用，判断层级结构是否已存在
        public void AddNodeBefore(glTFNode node, Element e, ElementType type,SocketRequestParam requestParam, List<glTFParameterGroup> attributes)
        {
            // 基本分类信息
            //var floor = GetLevel(e);
            var floor = glTFUtil.GetLevelParameter(e);
            var category = e.Category.Name;
            var family = type.FamilyName;
            var familyType = type.Name;

            // 标高层级
            FloorGroup floor_g = new FloorGroup
            {
                Name = floor,
                Children = new List<CategoryGroup> { }
            };
            if (!floorMap.ContainsKey(floor))
            {
                tree["root"].Children.Add(floor_g);

                glTFNode fNode = new glTFNode();
                fNode.name = floor;

                _glTF.nodes.Add(fNode);
                _glTF.nodes[0].children.Add(_glTF.nodes.Count - 1);

                fNode.children = new List<int>();
                floorMap.Add(floor, fNode);
            }
            else
            {
                var mid = tree["root"].Children.Find(x => x.Name == floor);
                if (mid != null)
                {
                    floor_g = mid;
                }
            }

            // 分类层级
            var cg = new CategoryGroup
            {
                Name = category,
                Children = new List<FamilyGroup> { }
            };
            if (!categoryMap.ContainsKey(floor + category))
            {
                floor_g.Children.Add(cg);

                glTFNode cNode = new glTFNode();
                cNode.name = category;

                _glTF.nodes.Add(cNode);
                floorMap[floor].children.Add(_glTF.nodes.Count - 1);

                cNode.children = new List<int>();
                categoryMap.Add(floor + category, cNode);
            }
            else
            {
                var mid = floor_g.Children.Find(x => x.Name == category);
                if (mid != null)
                {
                    cg = mid;
                }
            }

            var fg = new FamilyGroup
            {
                Name = family,
                Children = new List<FamilyTypeGroup> { }
            };
            if (!familyMap.ContainsKey(floor + category + family))
            {
                cg.Children.Add(fg);

                glTFNode fNode = new glTFNode();
                fNode.name = family;

                _glTF.nodes.Add(fNode);
                categoryMap[floor + category].children.Add(_glTF.nodes.Count - 1);

                fNode.children = new List<int>();
                familyMap.Add(floor + category + family, fNode);
            }
            else
            {
                var mid = cg.Children.Find(x => x.Name == family);
                if (mid != null)
                {
                    fg = mid;
                }
            }

            var ftg = new FamilyTypeGroup
            {
                Name = familyType,
                Children = new List<string> { }
            };
            if (!familyTypeMap.ContainsKey(floor + category + family + familyType))
            {
                fg.Children.Add(ftg);

                glTFNode ftNode = new glTFNode();
                ftNode.name = familyType;

                _glTF.nodes.Add(ftNode);
                familyMap[floor + category + family].children.Add(_glTF.nodes.Count - 1);

                ftNode.children = new List<int>();
                familyTypeMap.Add(floor + category + family + familyType, ftNode);
            }
            else
            {
                var mid = fg.Children.Find(x => x.Name == familyType);
                if (mid != null)
                {
                    ftg = mid;
                }
            }

            node.name = family + "[" + e.Id.IntegerValue + "]";
            familyTypeMap[floor + category + family + familyType].children.Add(_glTF.nodes.Count);
            //_glTF.nodes.Add(node);
            ftg.Children.Add(node.name);

            // 添加节点数组
            nodes.Add(new NodeArray
            {
                file_id = requestParam.fileId,
                name = $"{floor}-{category}-{family}-{familyType}",
                element_id = e.Id.IntegerValue,
                category = category,
                type = type.Name,
                attributes = attributes
            });
        }

        private string GetLevel(Element e)
        {
            string floor = "无标高组";

            // 获取元素标高 | 明细表标高 | 参照标高
            // SCHEDULE_LEVEL_PARAM: 参照标高，对于所有非系统族，当前参数是必有的，用于在明细表中区分标高
            // SCHEDULE_BASE_LEVEL_PARAM: 主要用于结构柱子的标高系统定义
            // INSTANCE_REFERENCE_LEVEL_PARAM: 参照标高，主要用于梁等元素的标高定义
            // FAMILY_LEVEL_PARAM:用于族对应的标高，长用于常规模型，和基于具有标高限制的主体的常规模型
            // FAMILY_BASE_LEVEL_PARAM:  主要用于“基于两个标高的公制常规模型”，其具有顶部和底部标高约束，并且可以根据顶部和底部偏移。
            var level = e.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);
            if (level == null)
            {
                level = e.get_Parameter(BuiltInParameter.SCHEDULE_BASE_LEVEL_PARAM);

                if (level == null)
                {
                    level = e.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM);

                    if (level == null)
                    {
                        level = e.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);

                        if (level == null)
                        {
                            level = e.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM);

                            if (level == null)
                            {
                                var levelId = e.LevelId;
                                if (e.LevelId != ElementId.InvalidElementId)
                                {
                                    Level l = e.Document.GetElement(levelId) as Level;
                                    floor = l.Name;
                                }
                            }
                        }
                    }
                }
            }
            
            if(level != null)
            {
                floor = level.AsValueString();
            }

            return floor;
        }

        public string ToJsonStr()
        {
            string jsonStr = JsonConvert.SerializeObject(tree, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
            return jsonStr;
        }

        public List<NodeArray> GetNodes()
        {
            return nodes;
        }

    }
}