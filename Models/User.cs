using System;
using System.Collections.Concurrent;
using System.Net;
using Common.Data;
using Common.Services;

namespace Common.Models
{
    public abstract class User
    {
        public readonly string UserName, Password;
        public readonly int Permission;

        

        public readonly byte[] Key;
        private string id;
        
        public string CurrentId
        {
            get => id;
            set { if (value == id) return; lCurrentId = id; id = value; }
        }



        protected User(ILogin login)
        {
            UserName = login.Username;
            Password = login.Pwd;
            Permission = login.Permission;
            Login = login;
            Client = login.Client;
        }
        

        public bool Check(RequestArgs serviceArgs)
        {
            return true;
        }

        public bool IsLogged, AllowSigninById = true;
        public IPAddress Address;
        public DateTime LastAccess;
        public ILogin Login { get; }
        public IClient Client { get; }
        public bool IsAdmin { get; protected set; }
        public bool IsAgent { get; protected set; }
        public bool IsClient { get; protected set; }
        

        public bool IsBlocked { get; set; }
        public string Identification { get; set; }
        public string lCurrentId { get; private set; }


        internal object GetCookie(string p, bool deleteIfExist)
        {
            if (cookies.TryGetValue(p, out Cookie cook))
            {
                if (cook.IsExpire)
                {
                    cookies.TryRemove(p, out cook);
                    return null;
                }
                if (deleteIfExist) cookies.TryRemove(p, out cook);
                return cook.value;
            }
            return null;
        }

        private ConcurrentDictionary<string, Cookie> cookies =
            new ConcurrentDictionary<string, Cookie>();

        internal void SetCookie(string p, object message, DateTime expire)
        {
            Cookie c;
            cookies.AddOrUpdate(p, c = new Cookie(message, expire), (o, n) => c);
        }
    }

    public class Cookie
    {
        public DateTime Expire;
        public object value;
        public bool IsExpire => DateTime.Now.Ticks > Expire.Ticks;

        public Cookie(object msg, DateTime expire)
        {
            value = msg;
            Expire = expire;
        }
    }
}
