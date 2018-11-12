using Common.Attributes;
using Common.Package;
using Common.Parsers;
using Common.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading;

namespace Common.Services
{
    public delegate void CommandCallback(TaskQueue<CommandsParam> queue, CommandsParam param);
    public class CommandsParam
    {
        public CommandsParam(RequestArgs args, CommandCallback callback, object param)
        {
            Args = args;
            Parms = param;
            Callback = callback;
        }
        public RequestArgs Args;
        public object Parms;
        public CommandCallback Callback;
    }


    public abstract partial class Server : CriticalFinalizerObject
    {
        private readonly Dictionary<string, Service> services = new Dictionary<string, Service>();
        public readonly Dictionary<string, Typeserializer> Serializers = new Dictionary<string, Typeserializer>();


        public abstract void BlockUser(RequestArgs args);

        public HttpListener Listener = new HttpListener();

        public Service GetService(string name) => services.TryGetValue(name.ToLower(), out Service s) ? s : null;

        public void AddService(Service service) => services[service.Name.ToLower()] = service;

        public readonly List<string> AddressEntries = new List<string>();
        private int connect(string[] addresses, bool first = true)
        {
            if (addresses == null || addresses.Length == 0) throw new ArgumentNullException(nameof(addresses));
            int connected = 0;
            List<string> nconnected = new List<string>();
            foreach (var _add in addresses)
            {
                var add = _add;
                try
                {
                    if (!add.EndsWith("/")) add += "/";
                    Listener.Prefixes.Add(add);
                    AddressEntries.Add(add);
                    Ionsole.WriteLine($"\t {add}");
                    connected++;
                }
                catch (Exception)
                {
                    nconnected.Add(add);
                }

            }
            if (nconnected.Count == 0)
                return connected;
            if (first)
            {
                var nsucc = 0;
                var computerName = Environment.GetEnvironmentVariable("COMPUTERNAME");
                var x = nconnected.ToArray(); nconnected.Clear();
                foreach (var add in x)
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = $@"http add urlacl url={add} user={computerName}\Administrator listen=yes",
                            CreateNoWindow = false,
                            ErrorDialog = false,
                            WindowStyle = ProcessWindowStyle.Hidden,
                            UseShellExecute = true,
                        }
                    };
                    p.Start();
                    p.WaitForExit();
                    if (p.ExitCode == 0)
                    {
                        nconnected.Add(add);
                    }
                    else nsucc++;
                }
                if (nconnected.Count > 0)
                {
                    return connect(nconnected.ToArray(), false);
                }
                Alert("The server is not initialized please check your fireWall");
                return 0;
            }
            return 0;
        }
        public abstract void Alert(string message, string title = null);
        private bool Initialize(string[] addresses)
        {
            try
            {
                Ionsole.WriteLine("--------------------------------");
                Ionsole.WriteLine("-----Loading Logings------------");
                ApiHandler.UpdateLogins(this.Database);
                Ionsole.WriteLine(new string('*', Ionsole.BufferWidth - 1));
                Ionsole.WriteLine("\r\n\r\n-----Finnish  !!!!--------------");
                Ionsole.WriteLine(new string('*', Ionsole.BufferWidth));

                Ionsole.WriteLine("-----Connecting-----------------");
                Listener.Realm = "http";
                Listener.Start();
                Ionsole.WriteLine("The Server Listen Into :");
                if (connect(addresses) == 0)
                    return false;
                Listener.BeginGetContext(Callback, this);

            }
            catch (Exception eo)
            {
                Ionsole.WriteLine("Error");
                Ionsole.WriteLine(eo.Message);
                return false;
            }
            Ionsole.WriteLine("--------Connect Now-------------");
            return true;

        }
        public static void GetIPAddress()
        {
            var strHostName = Dns.GetHostName();
            IPHostEntry ipEntry = Dns.GetHostEntry(strHostName);
            IPAddress[] addr = ipEntry.AddressList;
            for (int i = 0; i < addr.Length; i++)
            {
                Ionsole.WriteLine($"IP Address {i}: {addr[i].ToString()} ");
            }
        }
        public Context NewContext(bool isLocal, Data.DataBaseStructure database) => new Context(isLocal, database, this);

        protected Server()
        {
            apiTaskQuee = new TaskQueue<HttpListenerContext>(this.ProcessQuee, OnProcessQueeError);
            paquetQueue = new TaskQueue<IAsyncResult>(this.PacketProcess, OnProcessPacketError);
            CommandsQueue = new TaskQueue<CommandsParam>(this.CommandProcesser, this.OnCommandProcesserError);
        }
        public void Stop(Action callback)
        {
            StopServer();
            this.paquetQueue.ContinueWith((pq) =>
            {
                apiTaskQuee.ContinueWith(aq =>
                {
                    CommandsQueue.ContinueWith(cq =>
                    {
                        try { callback(); } catch { }
                    });
                });
            });
        }

        public Data.DataBaseStructure Database;
        public bool Start(string[] addresses, Data.DataBaseStructure database)
        {
            Database = database;
            return Initialize(addresses);
        }
        protected bool _pause;

        public abstract void GetFile(RequestArgs args);

        public virtual void Api(RequestArgs args) => args.Service?.Exec(args);

        public virtual void Dispose(Action callback = null)
        {
            try
            {
                Listener.Stop();
                Listener.Close();
                ApiHandler.Dispose();
                services.Clear();
                Serializers.Clear();
                paquetQueue.Dispose();
                CommandsQueue.Dispose();
                apiTaskQuee.Dispose();
            }
            catch { }
            callback?.Invoke();
        }

        private void Callback(IAsyncResult ar)
        {

            var c = TaskQueue<IAsyncResult>.CurrentTask;
            if (ar.CompletedSynchronously)
            {
                if (apiTaskQuee.Count > 200)
                    OnTropCharche(ar);
                else if (_pause)
                    this.ServerIsPaused(ar);
                else
                    paquetQueue.Add(ar);
            }
            if (!_pause)
                Listener.BeginGetContext(Callback, this);
        }

        protected virtual void ServerIsPaused(IAsyncResult ar)
        {

            try
            {
                //var x = Listener.EndGetContext(ar);
                //x.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                //RequestArgs.SendAlertError(x, "Wait ...", "Le serveur est en Wait Stat");
            }
            catch (Exception)
            {

            }
        }

        protected virtual void OnTropCharche(IAsyncResult ar)
        {
            try
            {
                var x = Listener.EndGetContext(ar);
                RequestArgs.SendAlertError(x, "Wait ...", "Le serveur est trop charger");
            }
            catch (Exception e)
            {
                Ionsole.Write(e);
            }
        }

        protected virtual bool OnRequest(HttpListenerContext context)
        {
            if (context.Request.HttpMethod == "OPTIONS")
            {
                ApisHandler.RespondOptions(context);
                return true;
            }
            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            var user = ApiHandler.CheckAuth(context, out bool logged);
            if (user != null || logged)
            {
                var serviceArgs = RequestArgs.NewRequestArgs(context, this, user);

                if (serviceArgs.Service == null)
                {
                    serviceArgs.SendCode(HttpStatusCode.OK);
                }
                else if (serviceArgs.Service.CanbeDelayed(serviceArgs))
                {
                    CommandsQueue.Add(new CommandsParam(serviceArgs, ExecuteCommand, this));
                    return false;
                }
                else
                    using (serviceArgs)
                    {
                        Api(serviceArgs);
                        return !serviceArgs.IsBusy;
                    }

            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
            return true;
        }

        private static void ExecuteCommand(TaskQueue<CommandsParam> queue, CommandsParam param)
        {
            var x = (Server)param.Parms;
            x.Api(param.Args);
        }


        public static void Send(HttpListenerContext context, byte[] x)
        {
            var r = context.Response;
            r.AddHeader("content-length", x.Length.ToString());
            Stream s = r.OutputStream;
            s.Write(x, 0, x.Length);
            s.Close();
        }

        public void AddSerializer(Typeserializer serializer) => Serializers[serializer.Name] = serializer;

        public ApisHandler ApiHandler;
        private TaskQueue<HttpListenerContext> apiTaskQuee;
        private TaskQueue<IAsyncResult> paquetQueue;
        public TaskQueue<CommandsParam> CommandsQueue;

        public static byte[] True = Encoding.UTF8.GetBytes("true");
        public static byte[] False = Encoding.UTF8.GetBytes("false");
        public const string SGuidService = "{{\"__service__\":\"guid\",\"sdata\":{0}}}";
        public readonly DateTime ExpiredTime = DateTime.Now + TimeSpan.FromDays(31);
        public readonly DateTime StartTime = DateTime.Now;
    }
    partial class Server
    {
        class PacketStat
        {
            public HttpListenerContext context;
            public RequestArgs args;
            public PacketStat(HttpListenerContext context)
            {
                this.context = context;
            }
        }

        protected virtual void PacketProcess(Operation<IAsyncResult> value)
        {
            HttpListenerContext context = Listener.EndGetContext(value.Value);
            value.Stat = context;
            var raw = context.Request.RawUrl;

            if (context.Request.HttpMethod == "OPTIONS")
            {
                ApisHandler.RespondOptions(context);
                return;
            }
            if (raw.Length > 2)
                if (raw[1] == '~')
                {
                    ApiHandler.PublicApi(context, raw);
                    goto fr;
                }
                else if (raw[1] == '_' && raw[2] == '/')
                {
                    apiTaskQuee.Add(context);
                    return;
                }
            if (Desktop.Default.GetResource(context.Request.Url.LocalPath.ToLowerInvariant(), out IResource file))
            {

                value.Stat = new PacketStat(context);
                if (file.RequireAuth)
                {
                    using (((PacketStat)value.Stat).args = RequestArgs.NewRequestArgs(context, this, null))
                        Desktop.Default.SendFileTo(((PacketStat)value.Stat).args, file);
                }
                else file.Reponse(context);
                return;
            }
            else
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;

            fr:
            context.Response.Close();

        }
        public void ProcessQuee(Operation<HttpListenerContext> sync)
        {
            HttpListenerContext context = sync.Value;
            if (OnRequest(context))
                context.Response?.Close();
        }

        private void OnProcessPacketError(Operation<IAsyncResult> c, Exception e)
        {

        }
        public void OnProcessQueeError(Operation<HttpListenerContext> sync, Exception e)
        {
            HttpListenerContext context = sync.Value;
            if (context != null)
            {
                //if (e.HResult != -2147467259)
                //    RequestArgs.SendAlertError(context, "Code Error : 0x" + e.HResult.ToString("x"), e.Message);
                context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
                context.Response.Close();
            }
        }
    }

    partial class Server
    {

        private void CommandProcesser(Operation<CommandsParam> value)
        {
            value.Value.Callback(CommandsQueue, value.Value);
        }

        private void OnCommandProcesserError(Operation<CommandsParam> value, Exception e)
        {
            value.Value.Args.context.Response.Close(False, false);
            value.Value.Args.Dispose();
            value.Value.Callback = null;
            value.Value.Parms = null;
        }

        public void AddToCriticalOperation(RequestArgs args, CommandCallback callback, object param) => CommandsQueue.Add(new CommandsParam(args, callback, param));
    }
    public partial class Server
    {
        public abstract bool CanContinueExecutingOperation();
        public void ResumeServer()
        {
            
            Listener.Start();
            _pause = false;
            Listener.BeginGetContext(Callback, this);
        }

        public void StopServer()
        {
            try
            {
                Listener.Stop();
                _pause = true;
                Ionsole.WriteLine("Server Listener Stoped Successfully");

            }
            catch { }
        }
    }
}
