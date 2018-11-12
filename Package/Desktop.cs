using Common.Services;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Common.Package
{

    [DataContract, Serializable]
    public partial class Desktop
    {
        public static Desktop Default => _default ?? (_default = new Desktop("Q-Desktop"));

        private AppInfo _current;
        private static Desktop _default;
        public Apps Apps { get; }
        public PackageInstaller Installer { get; }

        public event CurrentAPPHandler OnSelectedAppChanged;




        static string swap(AppInfo s) => (s?.AppName ?? "").Trim();
        public AppInfo SelectedApp
        {
            get { return _current; }
            set
            {
                if (value == null || value == _current) return;
                if (string.Compare(swap(value), swap(_current), true) == 0) return;
                var _old = _current;
                _current = value;
                LoadResources(value);
                OnSelectedAppChanged?.Invoke(this, _old, value);

            }
        }

        public void InstallDefaultApp()
        {
            var defaultApp = new AppInfo("Q-eShop", "", "Q-eShop.qapp");
            Installer.Install(defaultApp, ExtractExistingFileAction.OverwriteSilently);
            SelectedApp = defaultApp;
        }

        private Desktop(string name)
        {
            Name = name;
            Installer = new PackageInstaller(this);
            Apps = new Apps(this);
        }

        public AppInfo this[string name] => Apps[name];
        public System.IO.DirectoryInfo Path { get; } = new System.IO.DirectoryInfo(@"F:\Q-Test\eShop\Desktops\Q-Desktop\");

    }

    public partial class Desktop
    {
        public string Name { get; set; }
        private Dictionary<string, IResource> _files = new Dictionary<string, IResource>(100);

        public bool GetResource(string raw, out IResource resource) => _files.TryGetValue(raw, out resource);
        public void SendFileTo(RequestArgs args, IResource resource)
        {
            if (resource != null) resource.Reponse(args);
            else
            {
                args.context.Response.StatusCode = 404;
                args.context.Response.Close();
            }
        }
        public void SendFileTo(RequestArgs args, string file)
        {
            if (_files.TryGetValue(file, out var resource)) resource.Reponse(args);
            else
            {
                args.context.Response.StatusCode = 404;
                args.context.Response.Close();
            }
        }
        public void LoadResources(AppInfo app)
        {
            Utils.Ionsole.WriteLine("START Loading Manifest");
            var d = app.ResourcePath;
            _files.Clear();
            foreach (var rs in app.Resources)
                if (rs == null) continue;
                else
                {
                    var raw = rs.RawUrl.ToLowerInvariant();
                    if (rs.FileInfo.Extension == "") continue;
                    if (_files.ContainsKey(raw) == false)
                    {
                        _files.Add(raw, rs);
#if DEBUG
                        Utils.Ionsole.WriteLine(raw);
#endif
                    }
                    else Utils.Ionsole.WriteLine($"Duplicated entry detected raw:{raw} to file:{rs.filePath} ");
                }

            if (!_files.ContainsKey("/") && getEntryPoint(out var entry))
                _files.Add("/", entry);

            Utils.Ionsole.WriteLine("END Loading Manifest");
        }
        public void ReloadResources(AppInfo app)
        {
            Utils.Ionsole.WriteLine("START Loading Manifest");
            app.ReInitResources();
            var d = app.ResourcePath;
#if DEBUG
            if (app.Manifest.Exists) app.Manifest.Delete();
#endif
            _files.Clear();
            foreach (var rs in app.Resources)
                if (rs == null) continue;
                else
                {
                    var raw = rs.RawUrl.ToLowerInvariant();
                    if (rs.FileInfo.Extension == "") continue;
                    if (_files.ContainsKey(raw) == false)
                    {
                        _files.Add(raw, rs);
#if DEBUG
                        Utils.Ionsole.WriteLine(raw);
#endif
                    }
                    else Utils.Ionsole.WriteLine($"Duplicated entry detected raw:{raw} to file:{rs.filePath} ");
                }

            if (!_files.ContainsKey("/") && getEntryPoint(out var entry))
                _files.Add("/", entry);

            Utils.Ionsole.WriteLine("END Loading Manifest");
        }
        private bool getEntryPoint(out EntryPoint entry)
        {
            if (_files.TryGetValue(Envirenement.Variables.EntryPoint.ToLowerInvariant(), out var file) || _files.TryGetValue("/index.html", out file))
            {
                entry = new EntryPoint(file);
                return true;
            }
            entry = null;
            return false;
        }
    }
}
