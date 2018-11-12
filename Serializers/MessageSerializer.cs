using Common.Binding;
using Common.Data;
using Common.Models;
using Common.Parsers;
using Common.Parsers.Json;

namespace Common.Serializers
{
    public class MessageSerializer : DataRowTypeSerializer
    {
        private static DataTable<Message> messages = new Messages(null);
        public override DataTable Table => messages;

        public override bool CanBecreated => true;

        protected override JValue CreateItem(Context c, JValue jv)
        {
            return new Message(c, jv);
        }

        public override JValue ToJson(Context c, object ov)
        {
            return ov as Message;
        }

        public override void Stringify(Context c, object p)
        {
            (p as Message).Stringify(c);
        }

        public override void SimulateStringify(Context c, object p)
        {
            (p as Message).SimulateStringify(c);
        }

        public MessageSerializer()
            : base("models.Message")
        {
        }
        public override JValue Swap(Context c, JValue jv, bool requireNew)
        {
            var id = getId(jv);
            Message msg = messages[id];
            if (id == -1) return null;
            if (msg != null)
            {
                DObject.FromJson(msg, c, jv);
                return msg;
            }
            return new Message(c, jv);
            //return base.Swap(c, jv, requireNew);
        }

        public static void Register(Message msg)
        {
            messages[msg.Id] = msg;
        }

        public static Message UnRegister(long id)
        {
            return messages.Remove(id);
        }
        public static Message GetRegistration(long id)
        {
            return messages[id];
        }
    }
}
