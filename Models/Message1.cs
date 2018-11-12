using Common.Attributes;
using Common.Data;
using Common.Parsers;
using Common.Parsers.Json;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Models
{
    public enum MessageType
    {
        Info,
        Error,
        Alert,
        Confirm,
        Command
    }
    public delegate bool MSGHandler(RequestArgs args, Message msg, MessageHandler handler);
    public class MessageHandler
    {
        public object[] Params;
        public MSGHandler Action;
        public MessageHandler(MSGHandler action, params object[] Params)
        {
            this.Action = action;
            this.Params = Params;
        }
    }

    [HosteableObject(typeof(Api.Message), typeof(Serializers.MessageSerializer))]
    public class Message : DataRow
    {
        public new static int __LOAD__(int dp) => DPCancelText;
        private static int DPData = Register<Message, JValue>("Data");
        private static int DPTitle = Register<Message, string>("Title");
        private static int DPContent = Register<Message, string>("Content");
        private static int DPOKText = Register<Message, string>("OKText");
        private static int DPCancelText = Register<Message, string>("CancelText");

        public static int DPAbortText = Register<Message, string>("AbortText");

        //private static int DPResult = Register<Message, string>("Result");
        private static int DPAction = Register<Message, string>("Action");
        private static int DPType = Register<Message, MessageType>("Type");

        public string Title
        {
            get => get<string>(DPTitle);
            set => set(DPTitle, value);
        }
        public string Content
        {
            get => get<string>(DPContent);
            set => set(DPContent, value);
        }
        public string OKText
        {
            get => get<string>(DPOKText);
            set => set(DPOKText, value);
        }
        public string CancelText
        {
            get => get<string>(DPCancelText);
            set => set(DPCancelText, value);
        }
        public string AbortText { get => get<string>(DPAbortText); set => set(DPAbortText, value); }

        public JValue Data
        {
            get => get<JValue>(DPData);
            set => set(DPData, value);
        }


        public MessageType Type
        {
            get => get<MessageType>(DPType);
            private set => set(DPType, value);
        }

        public string Action => get<string>(DPAction);

        public MessageHandler ResponseHandler;
        public override void Dispose()
        {
            ResponseHandler = null;
            ResponseHandler.Action = null;
            ResponseHandler.Params = null;
            base.Dispose();
        }

        public Message()
        {
        }
        public Message(MessageType type)
        {
            Type = type;
        }

        public Message(Context c, JValue jv) : base(c, jv)
        {
        }

        public override JValue Parse(JValue json)
        {
            return json;
        }

        //public override string GetCreateTable()
        //{
        //    throw new System.NotImplementedException();
        //}


        public static Dictionary<Guid, Message> m = new Dictionary<Guid, Message>();
    }

    public class Messages : DataTable<Message>
    {
        public Messages(DataRow owner)
            : base(owner)
        {

        }
        protected override void GetOwner(DataBaseStructure d, Path c)
        {
            OwnerId = c.Id;
        }
        public long OwnerId { get; private set; }
        public Messages(Context c, JValue jv)
            : base(c, jv)
        {
        }

        public override JValue Parse(JValue json)
        {
            return json;
        }
    }

}
