using Common.Attributes;
using Common.Serializers;
using Common.Services;
using System;
using System.Threading;

namespace Common.Models
{
    [HosteableObject(typeof(GuidService), typeof(GuidSerializer))]
    public class GuidService : Service
    {
        public GuidService() : base("Guid")
        {

        }
        static object _syncObject = new object();

        public static long GetGuid()
        {
            lock (_syncObject)
            {
                var dt = DateTime.Now;
                var Y = dt.Year;
                var M = dt.Month;
                var D = dt.Day;
                var H = dt.Hour;
                var MN = dt.Minute;
                var S = dt.Second;
                var MS = dt.Millisecond;
                var id = (((long)((((Y - 2000) * 12 + M) * 31 + D) * 24 + H) * 60 + MN) * 60 + S) * 1000 + MS;
                Thread.Sleep(2);
                return id;
            }
        }

        public override bool Get(RequestArgs args)
        {
            var s = GetGuid();
            args.Send("[" + s + "," + s + 1000 + "]");
            return true;
        }
    }
}
