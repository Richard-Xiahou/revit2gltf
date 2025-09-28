using Revit2Gltf.socket;
using System;
using System.Collections.Generic;

namespace Revit2Gltf.utils
{
    /// <summary>
    /// 消息队列，单例模式
    /// </summary>
    internal class MessageStation
    {
        //构造函数私有化
        private MessageStation()
        {
            _messages = new Queue<KeyValuePair<int, SocketRequestParam>>();
        }

        // 饿汉式实现单例
        internal static readonly MessageStation Messages = new MessageStation();

        // 正在处理的消息
        internal static KeyValuePair<int, SocketRequestParam> CurrentWsMessage;

        // 消息处理程序
        internal static Action HandleMessageAction = null;

        // 队列信息读取处理的锁
        internal static bool ReadMsgLock
        {
            get { return Messages.readMsgLock; }
            set
            {
                if (!value && Messages.HasMessage && HandleMessageAction != null)
                {
                    HandleMessageAction();
                }

                Messages.readMsgLock = value;
            }
        }

        private bool readMsgLock = false;
        private Queue<KeyValuePair<int, SocketRequestParam>> _messages = null;

        //是否有消息
        internal bool HasMessage
        {
            get { return _messages.Count > 0; }
        }

        // 消息剩余数量
        internal int Count
        {
            get { return _messages.Count; }
        }

        //入列
        internal void AppendMessage(int clientId, SocketRequestParam message)
        {
            _messages.Enqueue(new KeyValuePair<int, SocketRequestParam>(clientId, message));

            if (!ReadMsgLock && HandleMessageAction != null)
            {
                HandleMessageAction();
            }
        }

        //出列
        internal KeyValuePair<int, SocketRequestParam> GetMessage()
        {
            if (_messages.Count > 0)
                return _messages.Dequeue();
            return new KeyValuePair<int, SocketRequestParam>(-1, new SocketRequestParam());
        }
    }
}
