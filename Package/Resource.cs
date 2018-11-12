using Common.Services;
using Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace Common.Package
{

    [Serializable]
    public class ReqHeader
    {
        public ReqHeader()
        {

        }
        public ReqHeader(string key, string value)
        {
            Key = key;
            Value = value;
        }
        [XmlAttribute]
        public string Key { get; set; }
        [XmlAttribute]
        public string Value { get; set; }
    }

    public interface IResource
    {
        string ContentType { get; set; }
        string Etag { get; set; }
        ReqHeader[] Headers { get; set; }
        string RawUrl { get; set; }
        bool RequireAuth { get; }
        byte[] GetBuffer();
        byte[] ReadGzipBuffer();

        void Reponse(HttpListenerContext c);

        bool CheckAccess(RequestArgs args);
        void Reponse(RequestArgs args);
    }

    [Serializable]
    [DataContract]
    public partial class Resource
    {
        public readonly static ReqHeader[] hdr = new ReqHeader[] { new ReqHeader("Service-Worker-Allowed", "/") };
        private static int i = 0;
        [DataMember(Name = nameof(App))] public AppInfo App { get; set; }
        [DataMember(Name = nameof(filePath))] public string filePath { get; set; }
        [DataMember(Name = nameof(RawUrl))] public string RawUrl { get; set; }
        [DataMember(Name = nameof(ContentType))] public string ContentType { get; set; }
        [DataMember(Name = nameof(Etag))] public string Etag { get; set; }
        [DataMember(Name = nameof(Cacheable))] public bool Cacheable { get; set; } = true;

        public ReqHeader[] Headers { get; set; }
        [NonSerialized, IgnoreDataMember]
        private byte[] _buffer;
        [XmlIgnore, IgnoreDataMember]
        private FileInfo _fileInfo;
        [XmlIgnore, IgnoreDataMember]
        private string _relativeFile;

        [XmlIgnore, IgnoreDataMember]
        private FileInfo _zipInfo;
        [XmlIgnore, IgnoreDataMember]
        private string _relativeZipFile;

        public Resource()
        {

        }
        public Resource(AppInfo appInfo, string filePath, string rawUrl, string contentType)
        {
            App = appInfo;
            this.ContentType = contentType;
            this.RawUrl = rawUrl;
            this.filePath = filePath;
            Etag = "35e674a2bf4bd" + i++ + ":0";
            var nm = filePath;
            if (nm.EndsWith("sw.js") || nm.EndsWith("sw.min.js")) Headers = Headers == null ? hdr : Enumerable.ToArray(Enumerable.Concat(Headers, hdr));
        }

    }

    public partial class Resource : IResource
    {
        [XmlIgnore, IgnoreDataMember]
        public FileInfo FileInfo
        {
            get
            {
                return _fileInfo ?? (_fileInfo = new FileInfo(Path.Combine(App.InstalationPath, filePath)));
            }
        }
        [XmlIgnore, IgnoreDataMember]
        public string RelativeFile => _relativeFile ?? (_relativeFile = FileInfo.FullName.GetRelativePath(App.ResourcePath.FullName));
        [XmlIgnore, IgnoreDataMember]
        public FileInfo GZipFileInfo => _zipInfo ?? (_zipInfo = new FileInfo(Path.Combine(App.ZipResourcePath.FullName, RelativeFile)));
        [XmlIgnore, IgnoreDataMember]
        public string RelativeGZipFile => _relativeZipFile ?? (_relativeZipFile = GZipFileInfo.FullName.GetRelativePath(App.ZipResourcePath.FullName));

        public bool RequireAuth => false;

        public byte[] GetBuffer()
        {
            if (!FileInfo.Exists) return new byte[0];
            try
            {
                return File.ReadAllBytes(FileInfo.FullName);
            }
            catch
            {
                return new byte[0];
            }
        }

        public byte[] ReadGzipBuffer()
        {
            if (!DEBUGGER.DisableCache)
            {
                if (Cacheable && _buffer != null) return _buffer;
                if (GZipFileInfo.Exists) return File.ReadAllBytes(GZipFileInfo.FullName);
            }
            byte[] _zipBuffer = RequestArgs.EncodeGZip(GetBuffer());
            try
            {
                if (!GZipFileInfo.Directory.Exists) GZipFileInfo.Directory.Create();
                File.WriteAllBytes(GZipFileInfo.FullName, _zipBuffer);
            }
            catch
            {
            }
            return DEBUGGER.DisableCache ? _zipBuffer : _buffer = _zipBuffer;
        }
        void Refuse(HttpListenerContext c)
        {
            c.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            c.Response.Close();
        }
        protected virtual void SetResposeHeader(HttpListenerContext x)
        {
            var r = x.Response;
            r.Headers.Add("content-type", ContentType);
            if (Headers != null)
                try
                {
                    for (int i = 0; i < Headers.Length; i++)
                        r.Headers.Add(Headers[i].Key, Headers[i].Value);
                }
                catch { }

            r.AddHeader("Cache-Control", "public");
            r.Headers.Add("max-age", TimeSpan.FromDays(365).Ticks.ToString());
            r.Headers.Add("Last-Modified", FileInfo.LastWriteTimeUtc.ToString());
            r.Headers.Add("date", DateTime.Now.ToString());
            r.Headers.Add("etag", Etag);
            r.Headers.Add(HttpResponseHeader.Expires, new DateTime(DateTime.Now.Ticks + TimeSpan.FromDays(365).Ticks).ToString());
            r.Headers.Add(HttpResponseHeader.Vary, "Accept-Encoding");
            r.AddHeader("Content-Encoding", "gzip");
        }

        public virtual void Reponse(HttpListenerContext c)
        {
            if (Cacheable && !DEBUGGER.DisableCache)
            {
                var lm = c.Request.Headers.Get("If-Modified-Since");
                if (lm != null)
                    if (DateTime.TryParse(lm, out var date) && date < FileInfo.LastWriteTime)
                    {
                        c.Response.StatusCode = (int)HttpStatusCode.NotModified;
                        return;
                    }
            }
            if (FileInfo.Exists)
            {
#if DEBUG
                if(this.FileInfo.Name.EndsWith("sw.js"))
                {
                    c.Response.StatusCode = 404;
                    c.Response.Close();
                    return;
                }
#endif
                var bytes = ReadGzipBuffer();
                if (bytes != null)
                {
                    SetResposeHeader(c);
                    var r = c.Response;
                    r.AddHeader("content-length", bytes.Length.ToString());
                    r.ContentLength64 = bytes.LongLength;
                    r.OutputStream.Write(bytes, 0, bytes.Length);
                    c.Response.Close();
                    return;
                }
            }
            c.Response.StatusCode = (int)HttpStatusCode.NotFound;
            c.Response.Close();
        }

        public virtual void Reponse(RequestArgs args)
        {
            if (!CheckAccess(args)) Refuse(args.context);
            else Reponse(args.context);
        }

        public virtual bool CheckAccess(RequestArgs args) => true;


    }
    [Serializable]
    public class AdminResource : Resource
    {
        public AdminResource()
        {

        }
        public AdminResource(AppInfo appInfo, string filePath, string rawUrl, string contentType) : base(appInfo, filePath, rawUrl, contentType)
        {
        }

        public override bool CheckAccess(RequestArgs a)
        {
            return a.User.IsAgent;
        }
    }
    [Serializable]
    public class SpecialResource : Resource
    {
        public SpecialResource()
        {

        }
        private int _perm;
        public SpecialResource(AppInfo app, int perm, string relativeFilePath, string rawUrl, string contentType) : base(app, relativeFilePath, rawUrl, contentType)
        {
            _perm = perm;
        }
        public override bool CheckAccess(RequestArgs a)
        {
            return (a.User.Permission & _perm) == _perm;
        }
    }

    [DataContract(IsReference = true)]
    [Serializable]
    public class EntryPoint : IResource
    {
        [DataMember(Name = nameof(Resource))]
        private IResource Resource;

        public string ContentType { get => Resource.ContentType; set => Resource.ContentType = value; }

        [DataMember(Name = nameof(Etag))]
        public string Etag { get => Resource.Etag; set => Resource.Etag = value; }

        public ReqHeader[] Headers { get => Resource.Headers; set => Resource.Headers = value; }

        public string RawUrl { get => "/"; set { } }

        public bool RequireAuth => Resource.RequireAuth;

        public EntryPoint(IResource resource)
        {
            Resource = resource;
        }
        public void Reponse(HttpListenerContext c)
        {
            c.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            Resource.Reponse(c);
        }
        public void Reponse(RequestArgs args)
        {
            args.context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            Resource.Reponse(args);
        }
        public bool CheckAccess(RequestArgs args)
        {
            return Resource.CheckAccess(args);
        }

        public byte[] GetBuffer()
        {
            return Resource.GetBuffer();
        }


        public byte[] ReadGzipBuffer()
        {
            return Resource.ReadGzipBuffer();
        }
    }
}

static class DEBUGGER
{
#if DEBUG
    public static bool DisableCache = !false;
#else
    public const bool DisableCache = false;
#endif
}