using Autodesk.Revit.UI;
using Revit2Gltf.socket;
using Revit2Gltf.utils;
using System.Collections.Generic;
using System;
using Revit2Gltf.http;

namespace Revit2Gltf.Event
{
    public class SocketMessageEvent : IExternalEventHandler
    {
        public void Execute(UIApplication uiApp)
        {
            if (MessageStation.Messages.HasMessage && !MessageStation.ReadMsgLock)
            {
                KeyValuePair<int, SocketRequestParam> message = MessageStation.Messages.GetMessage();
                if (message.Key == -1 || message.Value.filePath == "") { return; }

                try
                {
                    // 处理逻辑：打开rvt文件，查找命令开始执行转换
                    #region 在后台打开文件，UI上不会显示
                    //Document document = uiApp.Application.OpenDocumentFile(message.Value);
                    #endregion

                    #region 先关闭文件，再打开文件，在UI上显示
                    Common.CloseDocument(uiApp);

                    MessageStation.ReadMsgLock = true;
                    // 文件路径里的 \\ 转换为 \
                    message.Value.filePath = message.Value.filePath.Replace("\\\\", "\\");
                    MessageStation.CurrentWsMessage = message;

                    // 设置租户请求地址
                    if (message.Value.origin != null)
                    {
                        UrlConfig.SetBaseUrl(message.Value.origin);
                        Log.Debug($"[Progress]  设置租户请求地址:{message.Value.origin}");
                    }

                    // 回复
                    Common.SendSocketStart(MessageStation.CurrentWsMessage);

                    uiApp.OpenAndActivateDocument(MessageStation.CurrentWsMessage.Value.filePath);
                    #endregion
                }
                catch (Exception)
                {
                }
            }
        }

        public string GetName()
        {
            return "SocketMessageEvent";
        }
    }
}