using Common.Attributes;
using Common.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Models
{

    [HosteableObject(typeof(SessionIdService))]
    public class SessionIdService : Service
    {
        private static Guid _gsessionId = Guid.NewGuid();
        private static string _sessionID = "\"" + _gsessionId.ToString("N") + "\"";
        public static string SessionId
        {
            get
            {
                return _sessionID;
            }
        }
        public static Guid Guid { get { return _gsessionId; } }
        public SessionIdService() : base("SessionId")
        {
        }
        static object _syncObject = new object();


        public override bool Get(RequestArgs args)
        {
            args.Send(SessionId);
            return true;
        }
    }
}
