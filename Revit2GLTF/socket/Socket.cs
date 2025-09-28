using Fleck;
using Newtonsoft.Json.Linq;
using Revit2Gltf.utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Revit2Gltf.socket
{
    internal class Socket
    {
        private int target_port = 8081;
        public string target_host = "127.0.0.1";
        public WebSocketServer server_socket = null;
        //存储连接对象的池
        public List<IWebSocketConnection> allSockets = new List<IWebSocketConnection>();

        internal Socket() { }

        internal Socket(string ip, int port)
        {
            target_host = ip;
            target_port = port;
        }

        internal int Port
        {
            get { return target_port; }
            set { target_port = value; }
        }

        internal string Host
        {
            get { return target_host; }
            set { target_host = value; }
        }


        //建立与客户端的连接
        internal bool StartListening()
        {
            try
            {
                //创建webscoket服务端实例
                server_socket = new WebSocketServer($"ws://{target_host}:{target_port}");
                var index = 0;
                server_socket.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        allSockets.Add(socket);
                        index = allSockets.Count() - 1;
                    };
                    socket.OnClose = () =>
                    {
                        allSockets.Remove(socket);
                    };
                    socket.OnMessage = message =>
                    {
                        try
                        {
                            PlatformList platformList = new PlatformList
                            {
                                BomFlow = "10317b71ba277f5b9bf07ab50cde6230",
                                Manual = "4cd00be7fec9a48ced903d592b0f5e7b",
                                Other = "9d44481715156c41bb84eb6f962252d6"
                            };

                            JObject msgJson = JObject.Parse(message);

                            bool match = IsPlatformIdMatch(msgJson["platformID"].ToString(), platformList);
                            if (match == false)
                            {
                                Log.Error("参数错误,platformID不在信任列表内。");
                                return;
                            }

                            // 判断fileId和filePath是否存在
                            if (msgJson.Property("fileId") == null || msgJson.Property("filePath") == null || msgJson.Property("sessionId") == null)
                            {
                                Log.Error("参数错误,缺少fileId/sessionId/filePath");
                                return;
                            }

                            SocketRequestParam requestParam = new SocketRequestParam();
                            requestParam.platformID = msgJson["platformID"].ToString();
                            requestParam.fileId = (int)msgJson["fileId"];
                            requestParam.sessionId = msgJson["sessionId"].ToString();
                            requestParam.filePath = msgJson["filePath"].ToString();
                            if (msgJson.Property("identityID") != null)
                            {
                                requestParam.identityID = msgJson["identityID"].ToString();
                            }

                            if (msgJson.Property("options") != null && !string.IsNullOrWhiteSpace(msgJson.Property("options").ToString()))
                            {
                                requestParam.options = new SocketTransformSetting(msgJson["options"]);
                                Log.Debug($"[socket] options:{requestParam.options}");
                            }
                            else
                            {
                                requestParam.options = new SocketTransformSetting();
                            }

                            if (msgJson.Property("origin") != null)
                            {
                                requestParam.origin = msgJson["origin"].ToString();
                            }

                            if (msgJson.Property("tenantToken") != null)
                            {
                                CookieHelper.SetCookie($"[{requestParam.origin}]{requestParam.fileId}", msgJson["tenantToken"].ToString());
                            }

                            requestParam.socketKey = index;
                            requestParam.socket = socket;
                            //客户端消息入列
                            MessageStation.Messages.AppendMessage(index, requestParam);

                            Log.Debug($"[socket] 开始轻量化文件:{requestParam.origin},{requestParam.filePath}");

                        }
                        catch (Exception e)
                        {
                            Log.Error($"参数错误,请传入json字符串! Error: {e.Message}");
                        }
                    };
                });

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 方法用于判断给定的JSON对象中的"platformID"是否匹配PlatformID类实例的任何一个平台ID
        private bool IsPlatformIdMatch(string id, PlatformList list)
        {
            // 分别与PlatformID实例的BomFlow、Manual、Other属性值比较
            return list.BomFlow.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                   list.Manual.Equals(id, StringComparison.OrdinalIgnoreCase) ||
                   list.Other.Equals(id, StringComparison.OrdinalIgnoreCase);
        }
        internal void Send(int socketKey, string msg)
        {
            allSockets[socketKey].Send(msg);
        }

        internal void Send(IWebSocketConnection socket, string msg)
        {
            socket.Send(msg);
        }
    }
}
