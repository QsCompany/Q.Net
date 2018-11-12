using Common.Crypto;
using Common.Data;
using Common.Models;
using Common.Parsers;
using Common.Parsers.Json;
using Common.Serializers;
using Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;

namespace Common.Services
{
    public class RequestArgs : IDisposable
    {

        static readonly byte[] fail = { 123, 34, 95, 95, 115, 101, 114, 118, 105, 99, 101, 95, 95, 34, 58, 110, 117, 108, 108, 44, 34, 105, 115, 115, 34, 58, 102, 97, 108, 115, 101, 125 };
        static readonly byte[] success = { 123, 34, 95, 95, 115, 101, 114, 118, 105, 99, 101, 95, 95, 34, 58, 110, 117, 108, 108, 44, 34, 105, 115, 115, 34, 58, 116, 114, 117, 101, 125 };



        private static Stack<RequestArgs> _store = new Stack<RequestArgs>();
        public string Url;
        public User User;
        public Server Server;
        public HttpListenerContext context;

        public string[] Path;
        public Service Service;
        public bool IsFile;
        public string Method;
        private JValue _bodyASJson;
        private Context _jcontext;
        public readonly Dictionary<string, string> Params = new Dictionary<string, string>();

        protected RequestArgs(string method, Service service, JValue value, RequestArgs parent)
        {
            Url = parent.Url;
            User = parent.User;
            Server = parent.Server;
            context = parent.context;
            Path = parent.Path;
            Service = service;
            Method = method.ToUpper();
            _bodyASJson = value;
            _jcontext = parent._jcontext;

        }

        public static AesCBC aes = new AesCBC(new byte[32] { 234, 23, 196, 234, 69, 238, 92, 244, 50, 110, 70, 181, 109, 139, 252, 209, 146, 174, 40, 140, 129, 41, 58, 89, 102, 193, 99, 194, 178, 192, 239, 152 });

        public bool CodeError(int code = 401, bool success = false)
        {
            context.Response.StatusCode = code;
            return success;
        }
        protected RequestArgs(HttpListenerContext c, Server s, User u)
        {
            Update(c, s, u);
        }
        private RequestArgs Reset()
        {
            isBusy = false;
            issended = false;
            Method = context.Request.HttpMethod.ToUpper();
            var h = context.Request.Headers.ToString();
            Params.Clear();
            JContext.Reset();
            JContext.ResetParameterSerialiers();
            _bodyASJson = null;
            var rawurl = context.Request.RawUrl.Split('?');
            var raw = rawurl[0].Split('/');
            if (rawurl.Length > 1)
                UpdateParams(rawurl[1]);
            if (raw[1] == "_" || raw[1] == "Login")
            {
                Path = new string[raw.Length - 2];
                Array.Copy(raw, 2, Path, 0, Path.Length);
                IsFile = false;
                if (Path.Length > 0)
                    Service = Server.GetService(Path[0]);
            }
            else
            {
                IsFile = true;
                Path = null;
                Service = null;
            }
            _bodyASJson = null;
            _bytes = null;
            return this;
        }

        private void callrequest(IAsyncResult ar)
        {
            var cont = ar.AsyncState as HttpListenerContext;
            var cert = cont.Request.EndGetClientCertificate(ar);
        }

        private void UpdateParams(string @params)
        {
            var ps = @params.Split('&');
            for (var i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                var t = p.Split('=');
                var name = t[0];
                var value = t.Length > 1 ? t[1] : "";
                Params[name] = value;
            }
        }
        public static RequestArgs NewRequestArgs(HttpListenerContext c, Server s, User u)
        {
            if (_store.Count != 0)
                try
                {
                    lock (_store)
                        return _store.Pop().Update(c, s, u);
                }
                catch (Exception)
                {
                }
            return new RequestArgs(c, s, u);
        }
        private RequestArgs Update(HttpListenerContext c, Server s, User u)
        {
            this.isBusy = false;
            Url = c.Request.RawUrl;
            context = c;
            Server = s;
            User = u;
            Database = s.Database;
            Reset();
            return this;
        }
        public void Dispose()
        {
            if (this.isBusy) return;
            _store.Push(this);

        }

        public Context JContext => _jcontext ?? (_jcontext = this.Server.NewContext(false, Database));

        public static byte[] ReadStream(Stream s)
        {
            var buffer = new byte[2048];
            using (var output = FastArray.New(2024))
                do
                {
                    var i = s.Read(buffer, 0, buffer.Length);
                    if (i == 0) return output.ToArray();
                    output.AddRange(buffer, i);
                } while (true);
        }
        public static bool https = false;
        public JValue BodyAsJson
        {
            get
            {
                if (_bodyASJson != null) return _bodyASJson;

                var str = (context.Request.ContentEncoding ?? Encoding.Default).GetString(BodyAsBytes);
                return _bodyASJson = JContext.Read(str, true);
            }
            set
            {
                _bodyASJson = value;
            }
        }
        private byte[] _bytes;
        public byte[] BodyAsBytes
        {
            get
            {
                if (_bytes != null) return _bytes;
                if (https) aes.Key = User?.Key ?? new byte[32] { 234, 23, 196, 234, 69, 238, 92, 244, 50, 110, 70, 181, 109, 139, 252, 209, 146, 174, 40, 140, 129, 41, 58, 89, 102, 193, 99, 194, 178, 192, 239, 152 };

                var data = ReadStream(context.Request.InputStream);
                return _bytes = https ? aes.Decrypt(data) : data;
            }
        }
        private bool issended;
        public static void Send(HttpListenerContext context, byte[] x)
        {
            var r = context.Response;
            r.AddHeader("content-length", x.Length.ToString());
            var s = r.OutputStream;
            s.Write(x, 0, x.Length);
            s.Close();
        }
        public static void Send(HttpListenerContext context, string json)
        {
            context.Response.AddHeader("content-type", "text/plain; charset=Windows-1252");
            Send(context, context.Request.ContentEncoding.GetBytes(json ?? ""));
        }
        public void Send(byte[] x)
        {
            if (issended) return;
            issended = true;
            var r = context.Response;
            r.AddHeader("content-length", x.Length.ToString());
            var s = r.OutputStream;
            s.Write(x, 0, x.Length);
            s.Close();
        }

        public static byte[] EncodeGZip(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                    zip.Write(buffer, 0, buffer.Length);
                buffer = ms.ToArray();
            }
            return buffer;
        }

        public static byte[] EncodeGZip(Stream buffer)
        {
            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    var i = 0;
                    var bfr = new byte[2048];
                    while ((i = buffer.Read(bfr, 0, bfr.Length)) > 0)
                    {
                        zip.Write(bfr, 0, i);
                    }

                }
                return ms.ToArray();
            }
        }
        public static byte[] DecodeGZip(Stream buffer)
        {
            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(buffer, CompressionMode.Decompress))
                {
                    var i = 0;
                    var bfr = new byte[217579];
                    while ((i = zip.Read(bfr, 0, bfr.Length)) > 0)
                    {
                        ms.Write(bfr, 0, i);
                    }
                }
                return ms.ToArray();
            }
        }
        public static byte[] DecodeGZip(byte[] buffer)
        {
            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(ms, CompressionMode.Decompress, true))
                    zip.Write(buffer, 0, buffer.Length);
                buffer = ms.ToArray();
            }
            return buffer;
        }

        public byte[] GZipSend(byte[] bytes, bool isZip = false)
        {
            //console.log("%c" + "Achour", "color:White; background:url('http://www.france24.com/favicon.ico'); line-height:1.5;padding:5px; font-size:50px; font-weight:bold;");
            var r = context.Response;
            if (!isZip)
            {
                using (var ms = new MemoryStream())
                {
                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                        zip.Write(bytes, 0, bytes.Length);
                    bytes = ms.ToArray();
                }
            }
            r.AddHeader("Content-Encoding", "gzip");
            r.ContentLength64 = bytes.Length;
            r.OutputStream.Write(bytes, 0, bytes.Length);
            return bytes;
        }

        public byte[] GZipSend(string s, bool isZip = false)
        {
            //console.log("%c" + "Achour", "color:White; background:url('http://www.france24.com/favicon.ico'); line-height:1.5;padding:5px; font-size:50px; font-weight:bold;");
            var r = context.Response;
            var bytes = Encoding.Default.GetBytes(s);
            r.AddHeader("content-type", "text/plain; charset=Windows-1252");
            if (!isZip)
            {
                using (var ms = new MemoryStream())
                {

                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                        zip.Write(bytes, 0, bytes.Length);
                    bytes = ms.ToArray();
                }
            }

            r.AddHeader("Content-Encoding", "gzip");
            r.ContentLength64 = bytes.Length;
            r.OutputStream.Write(bytes, 0, bytes.Length);
            return bytes;
        }
        public static byte[] GZipSend(HttpListenerContext context, byte[] bytes, bool isZip = false)
        {
            var r = context.Response;
            if (!isZip)
            {

                using (var ms = new MemoryStream())
                {
                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                        zip.Write(bytes, 0, bytes.Length);
                    bytes = ms.ToArray();
                }
            }
            r.AddHeader("Content-Encoding", "gzip");
            r.ContentLength64 = bytes.Length;
            r.OutputStream.Write(bytes, 0, bytes.Length);
            return bytes;
        }
        public static byte[] WriteGZip(HttpListenerResponse r, byte[] bytes, bool isZip = false)
        {
            if (!isZip)
            {

                using (var ms = new MemoryStream())
                {
                    using (var zip = new GZipStream(ms, CompressionMode.Compress, true))
                        zip.Write(bytes, 0, bytes.Length);
                    bytes = ms.ToArray();
                }
            }
            r.AddHeader("Content-Encoding", "gzip");
            r.ContentLength64 = bytes.Length;
            r.OutputStream.Write(bytes, 0, bytes.Length);
            return bytes;
        }
        public bool SendStatus(bool issuccess)
        {
            Send(issuccess ? success : fail);
            return issuccess;
        }
        public bool SendSuccess()
        {
            Send(success);
            return true;
        }

        public bool SendFail()
        {
            Send(fail);
            return false;
        }
        public static bool ActiveGZip = true;
        private bool isBusy;
        public bool IsBusy { get => isBusy; set => isBusy = value; }
        public void Send(object e)
        {
            if (e == null) return;
            JContext.GetBuilder().Clear();
            var s = JContext.Stringify(e).ToString();

            var x = Encoding.Default.GetBytes(s);

            context.Response.AddHeader("content-type", "text/plain; charset=Windows-1252");
            if (ActiveGZip)
                GZipSend(x);
            else Send(x);
        }
        public void Send(string json)
        {
            Send(context.Request.ContentEncoding.GetBytes(json ?? ""));
        }

        public static void SendError(HttpListenerContext context, string message)
        {
            var c = message;
            var sb = new StringBuilder(200);
            sb.Append("{\"__service__\":\"notification\",\"sdata\":{\"Content\":")
                .Append(JValue.StringifyString(message))
                .Append(",\"IsInfo\":true},\"iss\":false}");
            Send(context, context.Request.ContentEncoding.GetBytes(sb.ToString()));
        }

        public static void SendAlertError(HttpListenerContext context, string title, string msg, string okTxt = "OK")
        {
            var t = JValue.StringifyString(title);
            var c = JValue.StringifyString(msg);
            var ok = JValue.StringifyString(msg);

            var sb = new StringBuilder(200);
            sb
                .Append("{\"__service__\":\"alert\",\"sdata\":")
                .Append("{\"Content\":" + c + ",\"Title\":" + t + ",\"ok\":" + ok + "}");
            sb.Append(",\"iss\":false}");
            Send(context, sb.ToString());
        }
        public bool HasParam(string v) => Params.ContainsKey(v);

        public string GetParam(string p)
        {
            if (Params.TryGetValue(p, out p)) return p;
            return null;
        }
        public bool GetParam(string p, out long value)
        {
            value = -1;
            if (Params.TryGetValue(p, out p)) return long.TryParse(p, out value);
            return false;
        }

        public bool GetParam(string p, out float value)
        {
            value = -1;
            if (Params.TryGetValue(p, out p)) return float.TryParse(p, out value);
            return false;
        }

        public bool GetParam(string p, out double value)
        {
            value = -1;
            if (Params.TryGetValue(p, out p)) return double.TryParse(p, out value);
            return false;
        }
        public bool GetParam(string p, out DateTime? value)
        {
            value = null;
            if (Params.TryGetValue(p, out p)) if (DateTime.TryParse(p, out var dt)) { value = dt; return true; }
            return false;
        }
        public bool GetParam(string p, out DateTime value)
        {
            value = default(DateTime);
            if (Params.TryGetValue(p, out p))
            {
                if (long.TryParse(p, out var lg))
                {
                    value = lg.FromJSDate();
                    return true;
                }
                if (DateTime.TryParse(p, out value)) return true;
                try
                {
                    if (p == "NaN")
                    {
                        value = default;
                        return false;
                    }
                    if (DateTime.TryParse(Encoding.UTF8.GetString(Convert.FromBase64String(p)), out value)) return true;
                }
                catch { }


            }
            return false;
        }
        public bool GetParam(string p, out string value)
        {
            if (Params.TryGetValue(p, out value)) value = Uri.UnescapeDataString(value); else return false;
            return true;
        }
        public bool GetParam(string p, out int value)
        {
            value = -1;
            if (Params.TryGetValue(p, out p)) return int.TryParse(p, out value);
            return false;
        }

        public bool GetParam(string p, out bool value)
        {
            value = false;
            if (Params.TryGetValue(p, out p)) return bool.TryParse(p, out value);
            return false;
        }

        public bool GetParam(string p, out bool? value)
        {
            value = null;
            if (Params.TryGetValue(p, out p)) if (bool.TryParse(p, out var vl)) { value = vl; return true; }
            return false;
        }
        public bool GetParam(string p, out Guid value)
        {
            value = Guid.Empty;
            if (Params.TryGetValue(p, out p)) return Guid.TryParse(p, out value);
            return false;
        }
        public long Id
        {
            get
            {
                var id = GetParam("Id");
                if (id == null) return -1;
                if (long.TryParse(id, out var l)) return l;
                return -1;
            }
        }

        public void SetCookie(string key, string value)
        {
            var c = context.Response.Cookies[key];
            if (value == null)
            {
                if (c == null) return;
                c.Expired = true;
                c.Value = null;
            }
            else
            {
                context.Response.SetCookie(c = new System.Net.Cookie(key, value));

                c.Value = value;
                c.Expired = false;
                c.Expires = DateTime.Now + TimeSpan.FromDays(3);
                c.Domain = context.Request.Headers["Host"];
                c.Path = "/";
                context.Response.Cookies.Add(c);
            }
        }
        public bool SendAlert(string title, string msg, string okTxt = "OK", bool issuccess = true)
        {
            var t = JValue.StringifyString(title);
            var c = JValue.StringifyString(msg);
            var ok = JValue.StringifyString(msg);

            var sb = JContext.GetBuilder();
            sb
                .Append("{\"__service__\":\"alert\",\"sdata\":")
                .Append("{\"Content\":" + c + ",\"Title\":" + t + ",\"ok\":" + ok + "}");
            sb.Append(",\"iss\":").Append(issuccess ? "true}" : "false}");
            Send(sb.ToString());
            return issuccess;

        }
        
        public Message SendConfirm(string title, string msg, string okTxt = null, string cancleTxt = null, bool issuccess = true, JValue data = null)
        {
            JContext.Reset();
            var _msg = new Message(MessageType.Confirm)
            {
                Title = title,
                Content = msg,
                Data = data,
                OKText = okTxt ?? "",
                CancelText = cancleTxt ?? "",
                AbortText = ""
            };
            var sb = JContext.GetBuilder();
            sb.Append("{\"__service__\":\"confirm\",\"dropRequest\":true,\"sdata\":");
            _msg.Stringify(JContext);
            sb.Append(",\"iss\":").Append(issuccess ? "true}" : "false}");
            Send(sb.ToString());
            return _msg;
        }
        public Message SendSpeech(string title, string msg, string okTxt = null, string cancleTxt = null, string abortTxt = null, bool issuccess = true, JValue data = null, MessageHandler handler = null)
        {
            JContext.Reset();
            var _msg = new Message(MessageType.Confirm)
            {
                Title = title,
                Content = msg,
                Data = data,
                OKText = okTxt ?? "",
                CancelText = cancleTxt ?? "",
                AbortText = abortTxt ?? "",
                ResponseHandler = handler
            };
            var sb = JContext.GetBuilder();
            sb.Append("{\"__service__\":\"speech\",\"dropRequest\":true,\"sdata\":");
            _msg.Stringify(JContext);
            sb.Append(",\"iss\":").Append(issuccess ? "true}" : "false}");
            MessageSerializer.Register(_msg);
            Send(sb.ToString());
            return _msg;
        }

        public bool Update(DataRow f)
        {
            return Database.StrictSave(f, true);
        }
        public bool Insert(DataRow f)
        {
            return Database.StrictSave(f, false);
        }

        public bool SendCodeError(int codeError, bool issuccess)
        {
            var sb = JContext.GetBuilder();
            sb.Append("{\"__service__\":\"codeerror\",\"sdata\":")
                .Append(codeError)
                .Append(",\"iss\":").Append(issuccess ? "true}" : "false}");
            Send(sb.ToString());
            return issuccess;
        }

        public bool SendError(string codeName)
        {
            var c = Utils.CodeError.GetError(codeName);
            var sb = JContext.GetBuilder();
            sb.Append("{\"__service__\":\"notification\",\"sdata\":")
                .Append("{\"Content\":" + JValue.StringifyString(c.Message) + ",\"IsInfo\":true}")
                .Append(",\"iss\":").Append(c.IsSuccess ? "true}" : "false}");
            Send(sb.ToString());
            return c.IsSuccess;
        }
        public bool SendError(string msg, bool issuccess)
        {
            var t = JValue.StringifyString(msg);
            var sb = JContext.GetBuilder();
            var e = "{\"Content\":" + t + ",\"IsInfo\":false}";
            sb.Append("{\"__service__\":\"notification\",\"sdata\":")
                .Append(e)
                .Append(",\"iss\":").Append(issuccess ? "true}" : "false}");
            Send(sb.ToString());
            return issuccess;
        }
        public bool SendInfo(string msg, bool issuccess)
        {
            var t = JValue.StringifyString(msg);
            var sb = JContext.GetBuilder();
            var e = "{\"Content\":" + t + ",\"IsInfo\":true}";
            sb.Append("{\"__service__\":\"notification\",\"sdata\":")
                .Append(e)
                .Append(",\"iss\":").Append(issuccess ? "true}" : "false}");
            Send(sb.ToString());
            return issuccess;
        }

        public bool SendCode(System.Net.HttpStatusCode code = HttpStatusCode.NotFound)
        {
            context.Response.StatusCode = (int)code;
            context.Response.Close();
            return code == HttpStatusCode.Accepted;
        }

        public DataBaseStructure Database { get; private set; }
    }
}



//c:\users\q-pc\source\repos\TestMSBuild\TestMSBuild.csproj
//F:\Test\MyTaskBuild\MSBuild.csproj
//c:\users\q-pc\source\repos\ConsoleApp1\ConsoleApp1.csproj