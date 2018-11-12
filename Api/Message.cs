using Common.Serializers;
using Common.Services;

namespace Common.Api
{
    public class Message : Service
    {
        public static string ActionTaken = "";
        public Message()
            : base("CallBack")
        {

        }

        public override bool Post(RequestArgs args)
        {
            args.JContext.RequireNew = (A, b) => true;
            var errr = args.BodyAsJson as Models.Message;
            if (errr != null)
            {
                var om = MessageSerializer.GetRegistration(errr.Id);
                if (om == null)
                    return args.SendFail();
                om.Data = errr.Data;
                if (om.ResponseHandler != null)
                {
                    om.ResponseHandler.Action(args, om, om.ResponseHandler);
                }
            }
            return false;
        }
        public override bool Get(RequestArgs args)
        {
            var data = new byte[32] { 234, 23, 196, 234, 69, 238, 92, 244, 50, 110, 70, 181, 109, 139, 252, 209, 146, 174, 40, 140, 129, 41, 58, 89, 102, 193, 99, 194, 178, 192, 239, 152 };
            var r = args.context.Response;
            r.Headers.Add("content-type", "application/octet-stream");
            args.GZipSend(data);
            return true;
        }
    }
}
