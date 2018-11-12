using Common.Api;
using Common.Data;
using Common.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;

namespace Common.Services
{
    public abstract class ApisHandler
    {
        protected Server server;
        
        protected static readonly Dictionary<string, User> Users = new Dictionary<string, User>();
        protected readonly Dictionary<string, User> _connectedUsers = new Dictionary<string, User>();

        public static void RespondOptions(HttpListenerContext context)
        {
            var r = context.Response;
            r.AddHeader("Access-Control-Expose-Headers", "id");
            r.AddHeader("Access-Control-Allow-Headers", "*");
            r.AddHeader("Access-Control-Allow-Methods", "*");
            r.AddHeader("Access-Control-Max-Age", "1728000");
            r.AppendHeader("Access-Control-Allow-Origin", "*");
            r.AddHeader("Cache-Control", "public");
            r.Headers.Add("Last-Modified", DateTime.Now.ToString(CultureInfo.InvariantCulture));
            r.Headers.Add("max-age", TimeSpan.FromDays(365).Ticks.ToString());
            r.Headers.Add(HttpResponseHeader.Expires, new DateTime(DateTime.Now.Ticks + TimeSpan.FromDays(365).Ticks).ToString(CultureInfo.InvariantCulture));
        }

        public virtual bool PublicApi(HttpListenerContext context, string raw)
        {

            switch (context.Request.Url.LocalPath.ToLower())
            {
                case "/~checklogging":
                    IsLoged(context);
                    break;
                case "/~login":
                    Login(context);
                    break;
                case "/~signup":
                    Signup(context);
                    break;
                case "/~signout":
                    Signout(context);
                    break;
                case "/~newGuid":
                    Server.Send(context, (context.Response.ContentEncoding ?? context.Request.ContentEncoding ?? Encoding.UTF8).GetBytes(Guid.NewGuid().ToString()));
                    break;
                case "/~guid":
                    var r = string.Format(Server.SGuidService, GuidService.GetGuid());
                    Server.Send(context, (context.Response.ContentEncoding ?? context.Request.ContentEncoding ?? Encoding.UTF8).GetBytes(r));
                    break;
                case "/~issecured":
                    Server.Send(context, RequestArgs.https ? Server.True : Server.False);
                    break;
                case "/~sessionid":
                    Server.Send(context, (context.Response.ContentEncoding ?? context.Request.ContentEncoding ?? Encoding.UTF8).GetBytes(SessionIdService.SessionId));
                    break;
                case "/~isadmin":
                    var cc = getId(context);
                    User user = null;
                    if (cc != null && (_connectedUsers.TryGetValue(cc, out user)))
                    {
                        if (user.IsBlocked)
                        {
                            _connectedUsers.Remove(cc);
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            break;
                        }
                        if (context.Request.RemoteEndPoint.Address.GetHashCode() != user.Address.GetHashCode())
                        {
                            if (AnotherAccountIsStillOpened(context, user))
                                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                            user.Address = context.Request.RemoteEndPoint.Address;
                        }
                        using (var rr = RequestArgs.NewRequestArgs(context, this.server, user))
                            if (user.IsAgent)
                                rr.SendSuccess();
                            else
                                rr.SendFail();
                    }
                    break;
                default:
                    if (raw.StartsWith("/~$?id") || raw.StartsWith("/~%24?"))
                        Downloader.Send(context);
                    else
                        return false;
                    break;
            }
            context.Response.Close();
            return true;
        }

        

        public ApisHandler(Server server)
        {
            this.server = server;
        }
        private void IsLoged(HttpListenerContext context)
        {
            var cc = getId(context);
            if (cc != null && (_connectedUsers.TryGetValue(cc, out User user)) && user.IsLogged)
            {
                user.LastAccess = DateTime.Now;
                context.Response.Close(Server.True, true);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentLength64 = Server.False.Length;
                context.Response.OutputStream.Write(Server.False, 0, Server.False.Length);
            }
        }

        public bool Logout(IClient client, RequestArgs args)
        {
            return _connectedUsers.Remove(args.User.UserName);
        }

        public abstract bool ValidateUser(RequestArgs args, ILogin e);
        public abstract User RegisterLogin(ILogin l);
        public abstract User UnRegisterLogin(ILogin l);

        public abstract bool DeleteUser(RequestArgs args, ILogin e);

        public abstract bool LockUser(RequestArgs args, ILogin e);

        public abstract bool SignupAgent(RequestArgs args, out User user);
        public virtual void Dispose() => _connectedUsers.Clear();
        public abstract User AdminUser { get; }

        public abstract void UpdateLogins(DataBaseStructure database);
        
        

        public virtual bool GetUserFromCookie(RequestArgs args, out User user)
        {
            var cc = getId(args.context);
            if (cc != null)
                if (_connectedUsers.TryGetValue(cc, out user) && user.AllowSigninById)
                {
                    if (cc == user.CurrentId)
                        return true;
                    foreach (System.Net.Cookie ck in args.context.Request.Cookies)
                    {
                        ck.Expired = true;
                        args.context.Response.SetCookie(ck);
                    }
                    user.IsLogged = false;
                }
            user = null;
            return false;
        }
        public virtual bool GetUserFromIdentAndData(RequestArgs args, out User user)
        {
            var login = (ILogin)args.BodyAsJson;
            if (login != null)
            {
                var identification = login.Identification;
                var username = login.Username;
                IPAddress ipaddress = IPAddress.None;

                var pssword = login.Pwd;
            deb:
                if (string.IsNullOrEmpty(username) == false)
                    if (Users.TryGetValue(username, out user))
                    {
                        return user.Password == pssword ? true : login.RegeneratePwd(user.Password);
                    }

                if (!string.IsNullOrEmpty(identification))
                {
                    var ds = RequestArgs.aes.Decrypt(identification).Split('\0');
                    if (ds.Length == 3)
                    {
                        username = ds[1];
                        pssword = ds[0];
                        if (IPAddress.TryParse(ds[2], out ipaddress))
                        {
                            identification = null;
                            goto deb;
                        }
                    }
                }
            }
            user = null;
            return args.SendAlert("Authentication", "Le Compt soit est desactiver ou est n'est pas enregistrer<br><br>Contacter l'admin", "OK", false);
        }


        public abstract ILogin CreateLogin(long id, string identification);
        public abstract User Login(HttpListenerContext context);
        public static string DisposeService = "{\"__service__\":\"notfication\"}";
        public static byte[] DisposeServiceBytes = Encoding.UTF8.GetBytes(DisposeService);
        #region Region 1

        public static string getId(HttpListenerContext context)
        {
            var cc = context.Request.Cookies["id"]?.Value;
            if (cc != null) return cc;
            cc = context.Request.Headers["xreq"];
            if (cc == null) return null;
            try
            {
                cc = Encoding.UTF8.GetString(Convert.FromBase64String(cc));
            }
            catch { }
            return cc.IndexOf(':') == 2 ? cc.Substring(3) : null;
        }
        public abstract User CheckAuth(HttpListenerContext context, out bool logged);

        public virtual bool AnotherAccountIsStillOpened(HttpListenerContext context, User user)
        {
            if ((DateTime.Now - user.LastAccess).TotalMinutes > 15)
                return false;

            var serviceArgs = RequestArgs.NewRequestArgs(context, this.server, user);
            var t = new SecurityAccountRequest
            {
                OriginalIP = user.Address?.ToString(),
                YourIP = context.Request.RemoteEndPoint.ToString(),
                Wait = 300000,
                IsSuccess = false
            };
            serviceArgs.Send(t);
            return true;
        }

        public abstract User Signout(HttpListenerContext context);
        public abstract User Signup(HttpListenerContext context);
        
        #endregion

    }
}
