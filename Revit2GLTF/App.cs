using System.Collections.Generic;
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using Revit2Gltf.glTF;
using Revit2Gltf.socket;
using Revit2Gltf.utils;
using System.Diagnostics;
using Newtonsoft.Json;
using Revit2Gltf.http;
using System.IO;
using Revit2Gltf.Event;

namespace Revit2Gltf
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    [Journaling(JournalingMode.NoCommandData)]

    /// <summary>
    /// 插件入口类，需实现IExternalApplication两个抽象函数：OnStartup和OnShutdown
    /// </summary>
    class App : IExternalApplication
    {
        private UIApplication uiApp = null;
        private Socket socket = null;

        // 自定义事件
        public ExternalEvent socketMessageEvent = null;
        // 视图查找次数
        private int viewFindCount = 0;
        // 文件打开成功与否
        private bool fileOpenSuccess = false;

        /// <summary>
        /// Revit启动时执行,挂载事件
        /// </summary>
        /// <param name="application">此参数类不提供访问Revit的功能</param>
        /// <returns></returns>
        public Result OnStartup(UIControlledApplication application)
        {
            #region Socket监听并执行命令
            //允许跨线程访问，忽略错误
            // System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;

            #region 创建并启动Socket监听
            socket = new Socket(UrlConfig.ScoketIp, UrlConfig.ScoketPort);
            socket.StartListening();
            #endregion

            application.ControlledApplication.ApplicationInitialized += OnApplicationInitialized;
            #endregion

            // 文件打开后的处理事件（此事件在Revit完成打开文档后立即引发。即使文档打开失败或被取消(在DocumentOpening事件期间)，也会引发该事件）
            application.ControlledApplication.DocumentOpened += DocumentOpened;
            // 文件关闭后的处理事件
            application.ControlledApplication.DocumentClosed += DocumentClosed;
            // 文件保存完的事件
            application.ControlledApplication.DocumentSaved += DocumentSaved;

            // 注册弹窗显示事件
            application.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(AppDialogShowing);

            // 注册自定义事件
            socketMessageEvent = ExternalEvent.Create(new SocketMessageEvent());

            // 注册ws消息处理方法
            MessageStation.HandleMessageAction = () =>
            {
                socketMessageEvent.Raise();
            };

            // 启用日志
            Log.Info("Revit启动，挂载完毕。");

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.ApplicationInitialized -= OnApplicationInitialized;
            application.ControlledApplication.DocumentOpened -= DocumentOpened;
            application.ControlledApplication.DocumentClosed -= DocumentClosed;
            application.ControlledApplication.DocumentSaved -= DocumentSaved;
            application.DialogBoxShowing -= new EventHandler<Autodesk.Revit.UI.Events.DialogBoxShowingEventArgs>(AppDialogShowing);

            return Result.Succeeded;
        }

        private void OnApplicationInitialized(object sender, ApplicationInitializedEventArgs e)
        {
            //获取UIApplication对象
            Application app = sender as Application;
            uiApp = new UIApplication(app);

            #region 视图激活事件
            uiApp.ViewActivated += HandleViewActivated;
            #endregion
        }

        // 文件打开后的处理
        private void DocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            viewFindCount = 0;

            // 消息读取锁住的时候才是websocket打开的文件
            if (MessageStation.ReadMsgLock && e.Status == RevitAPIEventStatus.Succeeded)
            {
                // 检查文件版本，是否需要升级
                Document doc = uiApp.ActiveUIDocument.Document;

                // 获取当前Revit应用程序的版本
                string version = uiApp.Application.VersionNumber;

                BasicFileInfo info = BasicFileInfo.Extract(doc.PathName);
                string fileVersion = info.Format;
                if (version != fileVersion)
                {
                    Log.Debug($"[Progress]  文件版本：${fileVersion},程序支撑版本：${version}");
                    Log.Debug($"[Progress]  版本不一致，文件已升级，正在保存文件...");

                    // 保存文件（文件有可能经过升级流程，防止二次打开再升级）
                    try
                    {
                        doc.Save();
                    }
                    catch (Exception ex)
                    {
                        fileOpenSuccess = true;
                        // 执行转GLTF导出前的准备工作
                        setoutExportGltf();
                    }
                }
                else
                {
                    fileOpenSuccess = true;
                    // 执行转GLTF导出前的准备工作
                    setoutExportGltf();
                }
            }
        }

        // 文件关闭后的处理
        private void DocumentClosed(object sender, DocumentClosedEventArgs e)
        {
            // 防止重复设置执行 MessageStation.ReadMsgLock.set
            if (MessageStation.ReadMsgLock)
            {
                Log.Debug("[Event] 关闭文件...");

                MessageStation.ReadMsgLock = false;
                MessageStation.CurrentWsMessage = new KeyValuePair<int, SocketRequestParam>(-1, new SocketRequestParam());
            }

            fileOpenSuccess = false;
            viewFindCount = 0;
        }

        // 文件保存完的事件
        private void DocumentSaved(object sender, DocumentSavedEventArgs e)
        {
            if (MessageStation.CurrentWsMessage.Value.filePath != "")
            {
                // 删除保存时的备份文件
                Common.DeleteBackupFile(MessageStation.CurrentWsMessage.Value.filePath);

                fileOpenSuccess = true;
                // 执行转GLTF导出前的准备工作
                setoutExportGltf();
            }
        }

        // 视图激活事件
        private void HandleViewActivated(object sender, ViewActivatedEventArgs e)
        {
            if (!MessageStation.ReadMsgLock) return;

            Log.Debug($"[Progress] 激活视图: {e.CurrentActiveView.Name}");
            setoutExportGltf();
        }

        // 消息处理完成
        private void HandleMessageCompleted()
        {
            MessageStation.ReadMsgLock = false;
            MessageStation.CurrentWsMessage = new KeyValuePair<int, SocketRequestParam>(-1, new SocketRequestParam());

            // 关闭已打开的
            Common.CloseDocument(uiApp);
        }

        private void setoutExportGltf()
        {
            // 文件是否准备好
            if (!fileOpenSuccess) return;

            Log.Debug("[Method]: setoutExportGltf()");

            try
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;
                if (uidoc == null)
                {
                    throw new Exception("文档是损坏的！");
                }
                Document doc = uidoc.Document;

                // 当前视图名称
                string currentViewName = doc.ActiveView.Name;

                // 判断需转换的视图
                if (MessageStation.CurrentWsMessage.Value.options.View == TransformSettingEnum.Default)
                {
                    // 先判断打开的是否是3D视图 及 当前视图是否是默认3D视图（Default3DView）
                    if (doc.ActiveView.GetType().Equals(typeof(View3D)) && (currentViewName.Equals("{三维}") || currentViewName.Equals("{3D}")))
                    {
                        // TODO：这时候界面还未显示对应视图，需要等待界面显示后再执行导出
                        Log.Debug($"[Progress]  已打开默认3D视图，开始处理导出...");
                        using (Transaction trans = new Transaction(doc, "Refresh Active View"))
                        {
                            trans.Start();

                            uidoc.RefreshActiveView();

                            trans.Commit();
                        }

                        exportGltf();

                        return;
                    }
                    else if (viewFindCount >= 1)
                    {
                        // 已经尝试打开过一次3D视图，但是仍然不是默认3D视图的话从视图列表中尝试查找
                        View3D view = Common.GetView3DByName(doc, "{三维}");
                        if (view == null)
                        {
                            view = Common.GetView3DByName(doc, "{3D}");
                        }
                        if (view == null)
                        {
                            viewFindCount = 0;
                            throw new Exception($"当前模型无法打开默认3D视图！");
                        }
                    }
                    else
                    {
                        Log.Debug("[Method]: OpenDefault3DView()");
                        // 打开 Default3DView,在 Idling 事件中无法执行成功
                        Common.OpenDefault3DView(doc, uiApp);

                        viewFindCount++;
                    }
                }
                else
                {
                    // 需要转换的视图名称
                    string viewName = MessageStation.CurrentWsMessage.Value.options.ViewName;
                    // 打开的是否是3D视图 及 当前视图是否是用户选择的视图
                    if (doc.ActiveView.GetType().Equals(typeof(View3D)) && viewName == currentViewName)
                    {
                        Log.Debug($"[Progress]  已打开视图:{currentViewName}，开始处理导出...");
                        exportGltf();
                        return;
                    }
                    else if (viewFindCount >= 1)
                    {
                        viewFindCount = 0;
                        throw new Exception($"当前模型无法打开视图：{viewName}！");
                    }
                    else
                    {
                        // 查找用户输入名称的视图
                        View3D view = Common.GetView3DByName(doc, viewName);
                        if (view == null)
                        {
                            throw new Exception($"当前模型不存在视图：{viewName}！");
                        }

                        // 异步切换为3D视图
                        uidoc.RequestViewChange(view);
                        viewFindCount++;
                        return;
                    }
                }
            }
            catch (Exception e)
            {
                Common.SendSocketFailMsg(MessageStation.CurrentWsMessage, 0.0, $"{e.Message}  详细错误：{e.ToString()}");

                HandleMessageCompleted();
            }
        }

        private void exportGltf()
        {
            viewFindCount = 0;

            bool isSuccess = false;
            Stopwatch stopWatch = new Stopwatch();
            //测量运行时间
            stopWatch.Start();

            try
            {
                UIDocument uidoc = uiApp.ActiveUIDocument;
                Document doc = uidoc.Document;

                SocketTransformSetting options = MessageStation.CurrentWsMessage.Value.options;

                /*using (TransactionGroup tg = new TransactionGroup(doc, "export glTF"))
                {
                    tg.Start();*/

                // 必须启动事务以设置视图上的参数:
                using (Transaction t = new Transaction(doc, "Set View ViewDetailLevel And DisplayStyle"))
                {
                    t.Start();

                    //视图的详细程度
                    doc.ActiveView.get_Parameter(BuiltInParameter.VIEW_DETAIL_LEVEL).Set((int)ViewDetailLevel.Fine);

                    // 视觉样式设置: 着色模式 | 真实模式 | 视图默认
                    if (options.DisplayStyle == TransformSettingEnum.ViewDefault)
                    {
                        options.DisplayStyle = doc.ActiveView.DisplayStyle == DisplayStyle.ShadingWithEdges ? TransformSettingEnum.Colour : TransformSettingEnum.Realistic;
                    }

                    t.Commit();
                }

                //在项目中截取错误/警告弹窗
                using (Transaction transaction = new Transaction(doc, "忽略错误"))
                {
                    try
                    {
                        /** 事务开始，对错误进行处理 **/
                        FailureHandlingOptions failTs = transaction.GetFailureHandlingOptions();
                        failTs.SetFailuresPreprocessor(new FailurePreprocessor());
                        transaction.SetFailureHandlingOptions(failTs);
                        transaction.Start();

                        #region 导出主体代码
                        string gltfPath = $"{Common.GetUtcNowTimeStamp()}.glb";
                        string fileName = $"{Path.GetDirectoryName(MessageStation.CurrentWsMessage.Value.filePath)}/{gltfPath}";
                        string bomdataPath = $"{Common.GetUtcNowTimeStamp()}.json";
                        string bomdataFileName = $"{Path.GetDirectoryName(MessageStation.CurrentWsMessage.Value.filePath)}/{bomdataPath}";

                        glTFStructureExportContext context = new glTFStructureExportContext(doc, fileName, MessageStation.CurrentWsMessage.Value);
                        CustomExporter exporter3d = new CustomExporter(doc, context);
                        exporter3d.IncludeGeometricObjects = false;
                        exporter3d.ShouldStopOnError = true;
                        exporter3d.Export(doc.ActiveView);

                        // 停止计时
                        stopWatch.Stop();

                        // 上传至oss
                        var sign = Api.UploadOss(fileName, "upload/drawingModel/rvt2glb", gltfPath);
                        // 上传成功后删除本地文件
                        Log.Debug($"[Progress]  上传至OSS完毕，删除文件：{fileName}");
                        File.Delete(fileName);

                        string bomdata = "";
                        if (context._bomTree != null)
                        {
                            bomdata = JsonConvert.SerializeObject(context._bomTree);
                            //bomdata = bomJsonString.Replace("\"", "'");  // 将双引号"替换为单引号'
                        }

                        string sceneTreeData = "";
                        if (context.sceneTree != null)
                        {
                            sceneTreeData = context.sceneTree.ToJsonStr();
                        }

                        // socket 回复
                        var sr = new SocketResponse();
                        sr.fileId = MessageStation.CurrentWsMessage.Value.fileId;
                        sr.sessionId = MessageStation.CurrentWsMessage.Value.sessionId;
                        sr.type = "completed";
                        sr.runSeconds = stopWatch.Elapsed.TotalSeconds;
                        sr.gltfPath = sign.url;
                        sr.filePath = MessageStation.CurrentWsMessage.Value.filePath;
                        sr.options = MessageStation.CurrentWsMessage.Value.options;
                        sr.errMsg = "";
                        sr.origin = MessageStation.CurrentWsMessage.Value.origin;
                        sr.platformID = MessageStation.CurrentWsMessage.Value.platformID;
                        sr.identityID = MessageStation.CurrentWsMessage.Value.identityID;
                        sr.bomdata = bomdata;
                        sr.sceneTreeData = sceneTreeData;
                        string jsonData = JsonConvert.SerializeObject(sr);
                        MessageStation.CurrentWsMessage.Value.socket.Send(jsonData);
                        #endregion

                        // 删除当前cookie
                        CookieHelper.ClearCurrentCookie();

                        isSuccess = true;
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        if (transaction.GetStatus() == TransactionStatus.Started)
                        {
                            // 回滚事务，丢弃更改
                            transaction.RollBack();
                        }

                        throw new Exception($"转换失败:{e.Message}  \r\n 详细：{e.ToString()}");
                    }
                    finally
                    {
                        transaction.Dispose();
                    }
                }

                /*tg.Commit();
            }*/
            }
            catch (Exception e)
            {
                // websocket 已回复成功时禁止错误回复
                if (!isSuccess)
                {
                    stopWatch.Stop();

                    Common.SendSocketFailMsg(MessageStation.CurrentWsMessage, stopWatch.Elapsed.TotalSeconds, $"{e.Message}");
                }
            }
            finally
            {
                stopWatch.Stop();

                HandleMessageCompleted();
            }
        }

        // 弹窗显示事件,在对话显示之前做一些工作
        private void AppDialogShowing(object sender, DialogBoxShowingEventArgs args)
        {
            // 非0值会处理掉不会显示 0会显示
            //args.OverrideResult(MessageStation.ReadMsgLock ? 1 : 0);
            args.OverrideResult(1);
        }
    }
}
