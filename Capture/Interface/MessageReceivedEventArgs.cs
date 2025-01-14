﻿using System;

namespace Capture.Interface
{
    [Serializable]
    public class MessageReceivedEventArgs
    {
        public MessageType MessageType { get; set; }
        public string Message { get; set; }

        public MessageReceivedEventArgs(MessageType messageType, string message)
        {
            MessageType = messageType;
            Message = message;
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", MessageType, Message);
        }
    }
}