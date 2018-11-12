using Common.Attributes;
using Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Common.Api
{

    public class FileId
    {
        public long UserId;
        public string File;
    }
    [HosteableObject(typeof(Downloader), null)]
    public class Downloader : Service
    {
        private static Dictionary<Guid, FileId> files = new Dictionary<Guid, FileId>();
        public Downloader() : base("$")
        {
        }
        public override bool Get(RequestArgs args)
        {
            var msg = "";
            if (!Guid.TryParse(args.GetParam("Id"), out var id)) msg = "The File Id Bad Format";
            if (!files.TryGetValue(id, out var fileId)) msg = "The file that you request is not vaid";
            if (!args.User.IsAgent && fileId.UserId != args.User.Client.Id) msg = "This file is not yours (haaa you cannot stolle me ever)";
            else
            {
                if (fileId == null) { msg = "The Dosn't Exist"; goto end; }
                var file = new FileInfo(fileId.File);
                var r = args.context.Response;
                if (file.Exists)
                {
                    r.Headers.Set("Content-Type", "application/pdf");
                    r.Headers.Add(HttpResponseHeader.Vary, "Accept-Encoding");
                    r.Headers.Set("Content-Transfer-Encoding", "binary");
                    r.ContentLength64 = file.Length;
                    r.AddHeader("Cache-Control", "public");
                    r.AddHeader("Expires", (file.LastWriteTimeUtc + TimeSpan.FromDays(111)).ToString());
                    args.Send(File.ReadAllBytes(file.FullName));
                }
                else return args.SendFail();
            }
        end:
            if (msg != null)
                return args.SendError(msg, false);
            return true;
        }

        public static FileId Set(Guid g, string filePath, long clientId)
        {
            var t = new FileId() { File = filePath, UserId = clientId };
            if (files.ContainsKey(g))
                files[g] = t;
            else files.Add(g, t);
            return t;
        }

        public static void Send(HttpListenerContext context)
        {
            var ids = context.Request.QueryString[0];
            if (!Guid.TryParse(ids, out var id)) return;
            if (!files.TryGetValue(id, out var fileId)) return;

            var file = new FileInfo(fileId.File);
            var r = context.Response;

            r.Headers.Set("Content-Disposition", "attachment; filename=\"" + file.Name + "\"");
            r.Headers.Set("Content-Type", "application/force-download");
            r.Headers.Set("Content-Transfer-Encoding", "binary");
            //r.Headers.Add(System.Net.HttpRequestHeader.ContentLength, file.Length.ToString());
            var t = File.ReadAllBytes(file.FullName);
            context.Response.ContentLength64 = file.Length;
            r.OutputStream.Write(t, 0, t.Length);
        }

        public static void Send(HttpListenerContext context, FileInfo file)
        {
            var r = context.Response;
            r.Headers.Set("Content-Disposition", "attachment; filename=\"" + file.Name + "\"");
            r.Headers.Set("Content-Type", "application/force-download");
            r.Headers.Set("Content-Transfer-Encoding", "binary");
            var t = File.ReadAllBytes(file.FullName);
            context.Response.ContentLength64 = file.Length;
            r.OutputStream.Write(t, 0, t.Length);
        }
    }
}
// https://forums.futura-sciences.com/physique/352711-relation-entre-joules-temperature-un-fil-de-cuivre.html