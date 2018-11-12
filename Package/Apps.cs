using Common.Parsers;
using Common.Parsers.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace Common.Package
{

    [Serializable]
    [DataContract(IsReference = true)]
    public class Apps : IEnumerable<AppInfo>
    {
        [XmlIgnore]
        private Desktop Desktop { get; }

        private readonly Dictionary<string, AppInfo> _list = new Dictionary<string, AppInfo>();

        public int Count => _list.Count;

        public Apps(Desktop desktop)
        {
            Desktop = desktop;
            Load();
        }
        public void Load()
        {
            _list.Clear();
#if DEBUG
            var f = new FileInfo(Path.Combine(Desktop.Path.FullName, "apps"));
#else
            var f = new FileInfo($@".\Desktops\{Desktop.Name}\apps");
#endif
            if (!f.Exists) return;
            try
            {
                var apps = File.ReadAllText(f.FullName);
                if (string.IsNullOrWhiteSpace(apps)) return;
                var c = new Context(true, null, null);
                if (!(c.Read(apps, false) is JArray arr)) return;
                foreach (var i in arr)
                    if (i is JString d)
                        try
                        {
                            Add(AppInfo.Load(d.Value));
                        }
                        catch { }
            }
            catch (Exception)
            {
            }
        }
        public void Save()
        {
            var o = new JArray();
            foreach (var app in this)
                o.Push((JString)app.InstalationPath);
            var c = new Context(true, null, null);
            o.ToJson(c);
            var f = new FileInfo($@".\Desktops\{Desktop.Name}\apps");
            if (f.Exists) f.Delete();
            if (!f.Directory.Exists) f.Directory.Create();
            File.WriteAllText(f.FullName, c.GetBuilder().ToString());
        }

        public void Delete(AppInfo app)
        {
            var i = this[app.AppName];
            if (i != null) { _list.Remove(app.AppName); return; }
            string _ = null;
            foreach (var p in _list)
            {
                if (p.Value == app)
                {
                    _ = p.Key;
                    break;
                }
            }
            if (_ == null) return;
            _list.Remove(_);
        }

        public IEnumerator<AppInfo> GetEnumerator() => _list.Select(a => a.Value).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public AppInfo this[string name]
        {
            get
            {
                name = name.ToLowerInvariant(); return _list.TryGetValue(name, out var x) ? x : null;
            }
            set
            {
                name = name.ToLowerInvariant();
                if (value == null)
                {
                    if (_list.ContainsKey(name))
                        _list.Remove(name);
                }
                else _list[name] = value;
            }
        }
        public AppInfo this[int index]
        {
            get
            {
                foreach (var x in _list)
                {
                    if (index == 0) return x.Value;
                    index--;
                }
                return null;
            }
        }

        public bool Add(AppInfo app)
        {
            if (app != null)
                this[app.AppName] = app;
            else return false;
            return true;
        }
    }
}
