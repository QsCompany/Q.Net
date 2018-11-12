using Common.Parsers;
using Common.Parsers.Json;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using static Common.Utils.Help;
namespace Common.Package
{

    [Serializable, DataContract(IsReference = true)]
    public partial class AppInfo
    {
        public string ToJson()
        {
            var c = new Context(true, null, null);
            Data.Stringify(c);
            return c.GetBuilder().ToString();
        }
        public static JObject FromJson(string json) => new Context(true, null, null).Read(json, false) as JObject;
        public JObject Data { get; private set; }
        public string AppName
        {
            get
            {
                string appName = Data[nameof(AppName)] as JString;
                return string.IsNullOrWhiteSpace(appName) ? getAppName() ?? "" : appName;
            }
        }
        public bool IsZip() => ZipFile.IsZipFile(AppPath.FullName);
        public string InstalationPath => Data[nameof(InstalationPath)] as JString;
        public string SourceAppFile => Data[nameof(SourceAppFile)] as JString;

        public AppInfo(JObject data = null) => Data = data ?? new JObject();
        public AppInfo Initalize(string appName, string instalationPath, string sourceAppFile)
        {
            Data[nameof(AppName)] = (JString)appName;
            Data[nameof(InstalationPath)] = (JString)instalationPath;
            Data[nameof(SourceAppFile)] = (JString)sourceAppFile;
            if (_resources != null) ReInitResources();
            return this;
        }
        public void SetInstalationPath(string path)
        {
            Data[nameof(InstalationPath)] = (JString)path;
            if (_resources != null) ReInitResources();
        }
        private DirectoryInfo _zipPath;
        public DirectoryInfo ZipResourcePath => _zipPath ?? (_zipPath = new DirectoryInfo(Path.Combine(InstalationPath, "__bin__")));

        public DirectoryInfo ResourcePath => new DirectoryInfo(string.IsNullOrWhiteSpace(InstalationPath) ? ".\\" : InstalationPath);
        public FileInfo AppPath => new FileInfo(SourceAppFile);

        public AppInfo(string appName, string instalationPath, string sourceAppFile) : this() => Initalize(appName, instalationPath, sourceAppFile);

        public string getAppName()
        {
            var n = AppPath?.Name;
            var dot = n == null ? -1 : n.LastIndexOf('.');
            n = dot == -1 ? n : n.Substring(0, dot);
            return n;
        }

        private Uri _folderUri;
        public Uri FolderUri => _folderUri ?? (_folderUri = new Uri(ResourcePath.FullName.EndsWith(Path.DirectorySeparatorChar.ToString()) ? ResourcePath.FullName : ResourcePath.FullName + Path.DirectorySeparatorChar));
        public string GetRelativePath(string path)
        {
            if (string.Compare(path + "\\", ResourcePath.FullName, true) == 0) return "";
            Uri pathUri = new Uri(path);
            return Uri.UnescapeDataString(FolderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        [DataMember(Name = nameof(Resources))]
        private List<Resource> _resources;

        public List<Resource> Resources => _resources ?? (_resources = ReInitResources());

        public List<Resource> ReInitResources()
        {
            if (_resources == null)
                _resources = new List<Resource>();
            else _resources.Clear();
            ProcessFolder(ResourcePath);
            return _resources;
        }
        private void ProcessFolder(DirectoryInfo d)
        {
            Resource l;
            foreach (var file in d.GetFiles())
                if ((l = New(file)) != null)
                    Resources.Add(l);

            foreach (var folder in d.GetDirectories())
            {
                if (string.Compare(folder.FullName, ZipResourcePath.FullName, true) == 0) continue;
                ProcessFolder(folder);
            }
        }

        internal Resource New(FileInfo file)
        {
            var relativePath = file.FullName.GetRelativePath(ResourcePath.FullName);
            var rawUrl = "/" + relativePath.Replace('\\', '/').Replace("//", "/").ToLowerInvariant();
            var ContentType = Envirenement.Data.GetContentType(file.Extension.ToLowerInvariant());
            var x = new Resource(this, relativePath, rawUrl, ContentType);
            x.filePath = x.RelativeFile;
            return x;
        }

        public void GenerateManifest()
        {
            var f = new FileInfo(Path.Combine(InstalationPath, nameof(Manifest)));
            if (!f.Exists) f.Create().Dispose();
            ReInitResources();
            using (var t = File.Open(f.FullName, FileMode.Truncate))
            {
                DataContractSerializer s = new DataContractSerializer(typeof(AppInfo), ExtraTypes.ToArray());

                s.WriteObject(t, this);
            }
        }

        public static AppInfo Load(string installPath)
        {
            var file = new FileInfo(Path.Combine(installPath, nameof(Manifest)));
            if (file.Exists)
                using (var t = File.OpenRead(file.FullName))
                {
                    try
                    {
                        DataContractSerializer s = new DataContractSerializer(typeof(AppInfo), ExtraTypes.ToArray());
                        var xc= s.ReadObject(t) as AppInfo;                            
                        return xc;
                    }
                    catch(Exception e) {
                        Common.Utils.Ionsole.WriteLine(file.FullName);
                    }
                }
            return null;
        }

        [XmlIgnore]
        public FileInfo Manifest => new FileInfo(Path.Combine(InstalationPath, nameof(Manifest)));
        public static List<Type> ExtraTypes = new List<Type> { typeof(AdminResource), typeof(Resource), typeof(SpecialResource), typeof(AppInfo), typeof(Desktop) };
    }

    public partial class AppInfo
    {
        [DataMember]
        private string DataJSON
        {
            get => ToJson();
            set => Data = FromJson(value);
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Data), ToJson());
            info.AddValue(nameof(Resources), Resources);
        }
        public AppInfo(SerializationInfo info, StreamingContext context)
        {
            Data = FromJson(info.GetString(nameof(Data)));
            _resources = info.GetValue(nameof(Resources), typeof(List<Resource>)) as List<Resource>;
        }

    }
}
