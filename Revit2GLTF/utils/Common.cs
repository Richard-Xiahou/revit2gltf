using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using Revit2Gltf.socket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Revit2Gltf.utils
{
    public class Common
    {
        /// <summary>
        /// 打开默认三维视图
        /// </summary>
        public static void OpenDefault3DView(Document doc, UIApplication uiapp)
        {
            using (Transaction trans = new Transaction(doc, "Open Default 3D View"))
            {
                trans.Start();
                //查找PostableCommand的API，在枚举中找到对应的方法为Default3DView
                RevitCommandId cmdid = RevitCommandId.LookupPostableCommandId(PostableCommand.Default3DView);
                if (uiapp.CanPostCommand(cmdid))
                {
                    uiapp.PostCommand(cmdid);
                }
                trans.Commit();
            }
        }

        public static void CloseDocument(UIApplication uiApp)
        {
            if (uiApp.ActiveUIDocument != null)
            {
                using (Transaction tran = new Transaction(uiApp.ActiveUIDocument.Document, "Close Document"))
                {
                    tran.Start();
                    // 关闭已打开的
                    RevitCommandId cmdid = RevitCommandId.LookupPostableCommandId(PostableCommand.Close);
                    if (uiApp.CanPostCommand(cmdid))
                    {
                        uiApp.PostCommand(cmdid);
                    }
                    tran.Commit();
                }
            }
        }

        /// <summary>
        /// 从文档中检索一个合适的3D视图。
        /// </summary>
        public static View3D Get3dView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));

            foreach (View3D v in collector)
            {
                // 此处跳过视图模板，因为视图模板在项目浏览器中不可见
                if (!v.IsTemplate)
                {
                    return v;
                }
            }
            return null;
        }

        /// <summary>
        /// 按名称获取3D视图
        /// </summary>
        public static View3D GetView3DByName(Document doc, string viewname)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(View3D));

            foreach (View3D v in collector)
            {
                // 此处跳过视图模板，因为视图模板在项目浏览器中不可见
                if (v.Name == viewname && !v.IsTemplate)
                {
                    return v;
                }
            }

            return null;
        }

        /// <summary>
        /// 按名称获取视图
        /// </summary>
        public static View GetViewByName(Document doc, string viewname)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(View));

            foreach (View v in collector)
            {
                // 此处跳过视图模板，因为视图模板在项目浏览器中不可见
                if (v.Name == viewname && !v.IsTemplate)
                {
                    return v;
                }
            }

            return null;
        }

        public static void SendSocketStart(KeyValuePair<int, SocketRequestParam> currentWsMessage)
        {
            var sr = new SocketResponse();
            sr.fileId = currentWsMessage.Value.fileId;
            sr.sessionId = currentWsMessage.Value.sessionId;
            sr.type = "start";
            sr.runSeconds = 0;
            sr.gltfPath = "";
            sr.filePath = currentWsMessage.Value.filePath;
            sr.options = currentWsMessage.Value.options;
            sr.errMsg = "";
            sr.origin = currentWsMessage.Value.origin;

            string jsonData = JsonConvert.SerializeObject(sr);
            currentWsMessage.Value.socket.Send(jsonData);

            Log.Debug($"[Start] origin:{sr.origin}   fileId:{sr.fileId}");
        }

        public static void SendSocketFailMsg(KeyValuePair<int, SocketRequestParam> currentWsMessage, double runSeconds, string msg)
        {
            var sr = new SocketResponse();
            sr.fileId = currentWsMessage.Value.fileId;
            sr.sessionId = currentWsMessage.Value.sessionId;
            sr.type = "fail";
            sr.runSeconds = runSeconds;
            sr.gltfPath = "";
            sr.filePath = currentWsMessage.Value.filePath;
            sr.options = currentWsMessage.Value.options;
            sr.errMsg = msg;
            sr.origin = currentWsMessage.Value.origin;

            string jsonData = JsonConvert.SerializeObject(sr);
            currentWsMessage.Value.socket.Send(jsonData);

            // 删除当前cookie
            CookieHelper.ClearCurrentCookie();

            Log.Debug($"[Fail] {jsonData}");
        }

        /// <summary>
        /// 获取测量点
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static Transform GetMeasuringPoint(Document doc)
        {
            // 获取项目原点位置
            ProjectPosition projectPosition = doc.ActiveProjectLocation.GetProjectPosition(XYZ.Zero);
            XYZ translationVector = new XYZ(projectPosition.EastWest, projectPosition.NorthSouth, projectPosition.Elevation);
            Transform translationTransform = Transform.CreateTranslation(translationVector);
            Transform rotationTransform = Transform.CreateRotation(XYZ.BasisZ, projectPosition.Angle);
            Transform finalTransform = translationTransform.Multiply(rotationTransform);

            return finalTransform;
            // 如果需要获取项目基点坐标，可以使用以下代码
            // return finalTransform.Origin;
        }

        /// <summary>
        /// 获取项目基点
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static Transform GetProjectBasePoint(Document doc)
        {
            double dNorthSouth = 0;
            double dEastWest = 0;
            double dElevation = 0;
            double dAngle = 0;
            ElementCategoryFilter siteCategoryfilter = new ElementCategoryFilter(BuiltInCategory.OST_ProjectBasePoint);
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> siteElements = collector.WherePasses(siteCategoryfilter).ToElements();
            foreach (var ele in siteElements)
            {
                Parameter paramNorthSouth = ele.get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM);
                if (paramNorthSouth != null)
                    dNorthSouth = paramNorthSouth.AsDouble();
                Parameter paramEastWest = ele.get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM);
                if (paramEastWest != null)
                    dEastWest = paramEastWest.AsDouble();
                Parameter paramElevation = ele.get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
                if (paramElevation != null)
                    dElevation = paramElevation.AsDouble();
                Parameter paramAngle = ele.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM);
                if (paramAngle != null)
                    dAngle = paramAngle.AsDouble();
            }

            XYZ projectBasePoint = new XYZ(dEastWest, dNorthSouth, dElevation);
            Transform translationTransform = Transform.CreateTranslation(projectBasePoint);
            Transform rotationTransform = Transform.CreateRotation(XYZ.BasisZ, dAngle);
            Transform finalTransform = translationTransform.Multiply(rotationTransform);

            return finalTransform;
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public static string GetUtcNowTimeStamp()
        {
            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds).ToString();
        }

        /// <summary>
        /// 去掉文件名中的非法字符
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string RemoveInvalid(string text)
        {
            string newText = "";
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                newText = text.Replace(invalidChar.ToString(), string.Empty);
            }

            return newText;
        }

        /// <summary>
        ///  删除备份文件
        /// </summary>
        /// <param name="filePath">文件全路径，包含文件后缀</param>
        public static void DeleteBackupFile(string filePath)
        {
            Task.Run(() =>
            {
                // 备份副本的名称为 <model_name>.<nnnn>.rvt，其中 <nnnn> 是表示该文件的保存次数的 4 位数字
                // 例如，如果文件名为“project.rvt”，则备份副本的名称为“project.0001.rvt”
                // 先判断并去除文件后缀
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string fileDirectory = Path.GetDirectoryName(filePath);

                // 正则表达式判断是否存在备份文件
                string pattern = fileName + @".\d{4}.rvt";
                string[] files = Directory.GetFiles(fileDirectory, fileName + ".*.rvt");
                foreach (string file in files)
                {
                    if (System.Text.RegularExpressions.Regex.IsMatch(file, pattern))
                    {
                        File.Delete(file);
                    }
                }
            });
        }
    }
}