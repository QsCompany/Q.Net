using System;
using System.IO;

using System.Net;
using System.Collections.Generic;
using Microsoft.Win32;


namespace Common.Envirenement
{
    public class RegValue
    {
        private object _value;
        public string Key;

        public object Value
        {
            get => _value;
            set => Reg.SetValue(Key, value == null ? GetDeafault(Kind) : _value = value);
        }
        public RegistryKey Reg;
        public RegistryValueKind Kind;
        public RegValue(RegistryKey reg, string name, RegistryValueKind kind, object value)
        {
            Reg = reg;
            Key = name;
            Kind = kind;
            Value = value;
        }

        public void Refrech()
        {
            _value = Swap(Reg.GetValue(Key, GetDeafault(Kind), RegistryValueOptions.None), Kind);
        }

        public static object GetDeafault(RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.String:
                    return "";
                case RegistryValueKind.Binary:
                    return new byte[0];
                case RegistryValueKind.DWord:
                    return 0;
                case RegistryValueKind.MultiString:
                    return new string[0];
                case RegistryValueKind.QWord:
                    return 0L;
                default:
                case RegistryValueKind.Unknown:
                case RegistryValueKind.None:
                    return null;
            }
        }

        public static object Swap(object p, RegistryValueKind kind)
        {
            switch (kind)
            {
                case RegistryValueKind.ExpandString:
                case RegistryValueKind.String:
                    return (p ?? "").ToString();
                case RegistryValueKind.Binary:
                    return p is byte[] ? p : p is string s ? System.Text.Encoding.Unicode.GetBytes(s) : p is int i ? BitConverter.GetBytes(i) : p is long l ? BitConverter.GetBytes(l) : null;
                case RegistryValueKind.DWord:
                    if (p is IConvertible c) return c.ToInt32(null);
                    if (p is string ss) return int.TryParse(ss, out int x) ? x : 0;
                    if (p is byte[] pp && pp.Length >= 4) return BitConverter.ToInt32(pp, 0);
                    return 0;
                case RegistryValueKind.MultiString:
                    if (p is string[]) return p;
                    if (p is string) return new string[] { (string)p };
                    if (p == null) return new string[0];
                    else return new string[] { p.ToString() };
                case RegistryValueKind.QWord:
                    if (p is IConvertible) return ((IConvertible)p).ToInt64(null);
                    if (p is string) return long.TryParse((string)p, out long x) ? x : 0L;
                    if (p is byte[] px && px.Length >= 8) return BitConverter.ToInt64(px, 0);
                    return 0L;
                default:
                case RegistryValueKind.None:
                case RegistryValueKind.Unknown:
                    return p;
            }
        }
    }
    public static partial class Variables
    {
        
        private static RegistryKey xreg = Registry.CurrentUser.CreateSubKey($@"SOFTWARE\{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name ?? "Q-DLL"}\{nameof(Variables)}");
        private static Dictionary<string, RegValue> _reg = new Dictionary<string, RegValue>(20);



        public static object GetValue(string key, bool cache, RegistryValueKind kind = RegistryValueKind.Unknown)
        {
            
            if (_reg.TryGetValue(key, out RegValue v)) if (cache) return v.Value;
                else
                    v.Refrech();
            else
                _reg[key] = v = new RegValue(xreg, key, kind, RegValue.Swap(xreg.GetValue(key), kind));
            return v.Value;
        }
        public static RegValue SetValue(string key, object value, RegistryValueKind kind = RegistryValueKind.Unknown)
        {
            if (!_reg.TryGetValue(key, out RegValue xv))
                _reg[key] = xv = new RegValue(xreg, key, value == null ? kind : GetValueKind(kind.GetType()), value);

            else
                xv.Value = value;
            return xv;
        }

        public static string GetString(string key) => (xreg.GetValue(key) ?? "").ToString();
        public static byte[] GetBinary(string key) => xreg.GetValue(key) as byte[] ?? new byte[0];
        public static string[] GetStrings(string key)
        {
            if (xreg.GetValueKind(key) == RegistryValueKind.String) return new string[] { (string)xreg.GetValue(key) ?? "" };
            return xreg.GetValue(key) as string[] ?? new string[0];
        }

        public static int GetInt(string key)
        {
            var v = xreg.GetValue(key);
            switch (xreg.GetValueKind(key))
            {
                case RegistryValueKind.DWord:
                    return (int)v;
                case RegistryValueKind.QWord:
                    return (int)(long)v;

                case RegistryValueKind.String:
                    if (int.TryParse((string)v ?? "", out int i)) return i;
                    return 0;
                case RegistryValueKind.Binary:
                    var p = (byte[])v ?? new byte[0];
                    if (p.Length >= 4) return BitConverter.ToInt32(p, 0);
                    return 0;
                case RegistryValueKind.None:
                case RegistryValueKind.Unknown:
                    return v is IConvertible d ? d.ToInt32(null) : 0;
                default:
                    return 0;
            }

        }
        public static long GetLong(string key)
        {

            var v = xreg.GetValue(key);
            switch (xreg.GetValueKind(key))
            {
                case RegistryValueKind.DWord:
                    return (int)v;
                case RegistryValueKind.QWord:
                    return (long)v;
                case RegistryValueKind.String:
                    if (long.TryParse((string)v ?? "", out long i)) return i;
                    return 0;
                case RegistryValueKind.Binary:
                    var p = (byte[])v ?? new byte[0];
                    if (p.Length >= 8) return BitConverter.ToInt64(p, 0);
                    return 0;
                case RegistryValueKind.None:
                case RegistryValueKind.Unknown:
                    return v is IConvertible d ? d.ToInt64(null) : 0;
                default:
                    return 0;
            }
        }
        public static void SetValue<T>(string key, T value) => xreg.SetValue(key, value, GetValueKind(typeof(T)));

        private static RegistryValueKind GetValueKind(Type type)
        {
            if (type == typeof(string)) return RegistryValueKind.String;
            if (type == typeof(string[])) return RegistryValueKind.MultiString;
            if (type == typeof(byte[])) return RegistryValueKind.Binary;
            if (type == typeof(int)) return RegistryValueKind.DWord;
            if (type == typeof(long)) return RegistryValueKind.QWord;
            return RegistryValueKind.Unknown;
        }


        private static DirectoryInfo _sharedPath;
        public static DirectoryInfo SharedPath
        {
            get
            {
                if (_sharedPath != null) return _sharedPath;
                var sp = _sharedPath;
                var p = GetString(nameof(SharedPath));
                if (string.IsNullOrWhiteSpace(p))
                {
                    p = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
                    sp = new DirectoryInfo(p);
                }
                else
                {
                    var f = new FileInfo(p);
                    if (f.Exists) sp = f.Directory;
                    else sp = new DirectoryInfo(p);
                }
                return _sharedPath = createDir(sp);
            }
            set
            {
                _sharedPath = value = createDir(value);
                xreg.SetValue(nameof(SharedPath), value.FullName);
            }
        }
        private static DirectoryInfo createDir(DirectoryInfo dir)
        {
            if (!dir.Exists)
                try
                {
                    dir.Create();
                }
                catch
                {
                    return new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments));
                }
            return dir;
        }


        private static DirectoryInfo _backupdir;
        public static DirectoryInfo BackupDir
        {
            get
            {
                if (_backupdir != null) return _backupdir;
                var sp = _backupdir;
                var p = GetString(nameof(BackupDir));
                if (string.IsNullOrWhiteSpace(p))
                {
                    p = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), "Backups");
                    sp = new DirectoryInfo(p);
                }
                else
                {
                    var f = new FileInfo(p);
                    if (f.Exists) sp = f.Directory;
                    else sp = new DirectoryInfo(p);
                }
                return _backupdir = createDir(sp);
            }
            set
            {
                _backupdir = value = createDir(value);
                xreg.SetValue(nameof(BackupDir), value.FullName);
            }
        }


        public static DateTime LastTimeBackup
        {
            get => DateTime.TryParse((string)GetValue(nameof(LastTimeBackup), true, RegistryValueKind.String), out var d) ? d : new DateTime(0);
            set => SetValue(nameof(LastTimeBackup), value.ToString(), RegistryValueKind.String);
        }

        public static string Or(string v1, string v2) => string.IsNullOrWhiteSpace(v1) ? v2 ?? "" : v1;


        private static DirectoryInfo _appsDir;
        public static DirectoryInfo AppsDir
        {
            get
            {
                if (_appsDir != null) return _appsDir;
                var sp = _appsDir;
                var p = GetString(nameof(AppsDir));
                if (string.IsNullOrWhiteSpace(p))
                {
                    p = Path.Combine(SharedPath.FullName, "Apps");
                    sp = new DirectoryInfo(p);
                }
                else
                {
                    var f = new FileInfo(p);
                    if (f.Exists) sp = f.Directory;
                    else sp = new DirectoryInfo(p);
                }
                return _appsDir = createDir(sp);
            }
            set
            {
                _appsDir= value = createDir(value);
                xreg.SetValue(nameof(AppsDir), value.FullName);
            }
        }

    }
    partial class Variables
    {
        private static DirectoryInfo mdp = new DirectoryInfo(Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles(x86)") ?? Environment.GetEnvironmentVariable("ProgramFiles"), @"MySQL\MySQL Server 5.2\bin\"));
        public static DirectoryInfo MySQLPath
        {
            get
            {
                var p = (string)GetValue(nameof(MySQLPath), true, RegistryValueKind.String);
                if (string.IsNullOrWhiteSpace(p)) return mdp;
                return new DirectoryInfo(p);
            }
            set => SetValue(nameof(MySQLPath), value?.FullName, RegistryValueKind.String);
        }

        public static IPAddress ServerIP
        {
            get => IPAddress.TryParse((string)GetValue(nameof(ServerIP), true, RegistryValueKind.String) ?? "127.0.0.1", out var ip) ? ip : IPAddress.Parse("127.0.0.1");
            set => SetValue(nameof(ServerIP), value == null ? "127.0.0.1" : value.ToString(), RegistryValueKind.String);
        }
        public static uint Port
        {
            get => Or((uint)Math.Min(Math.Max(0, (int)GetValue(nameof(Port), true, RegistryValueKind.DWord)), ushort.MaxValue), 3306u);
            set => SetValue(nameof(Port), (int)value, RegistryValueKind.DWord);
        }

        private static uint Or(uint v1, uint v2)
        {
            return v1 == 0 ? v2 : v1;
        }

        public static string UserID
        {
            get => Or((string)GetValue(nameof(UserID), true, RegistryValueKind.String), "root");
            set => SetValue(nameof(UserID), value, RegistryValueKind.String);
        }

        public static string Password
        {
            get => Or((string)GetValue(nameof(Password), true, RegistryValueKind.String), "root");
            set => SetValue(nameof(Password), value, RegistryValueKind.String);
        }

        public static string DatabasePath
        {
            get { return Or(GetString(nameof(DatabasePath)), "qshopdatabase"); }
            set { SetValue(nameof(DatabasePath), Or(value, "qshopdatabase")); }
        }

        public static string[] Addresses
        {
            get => (string[])GetValue(nameof(Addresses), true, RegistryValueKind.MultiString);
            set => SetValue(nameof(Addresses), value, RegistryValueKind.MultiString);
        }

        public static IPEndPoint[] IPEndPoints
        {
            get
            {
                var eps = new List<IPEndPoint>();
                foreach (var a in Addresses)
                {
                    var x = a.Split(':');
                    if (!IPAddress.TryParse(x[0], out var ip)) continue;
                    if (x.Length > 1 && !int.TryParse(x[1], out var p)) continue;
                    else p = 80;
                    eps.Add(new IPEndPoint(ip, p));
                }
                return eps.ToArray();
            }
        }

        public static string EntryPoint
        {
            get => Or((string)GetValue(nameof(EntryPoint), true, RegistryValueKind.String), "/").Replace('\\', '/');
            set => SetValue(nameof(EntryPoint) ?? "/", value, RegistryValueKind.String);
        }


    }

    public static class Data
    {
        public static string GetContentType(string ext)
        {
            return contentTypes.TryGetValue(ext, out ext) ? ext : "application/octet-stream";
        }
        public static bool TryGetContentType(string ext, out string contentType)
        {
            return contentTypes.TryGetValue(ext, out contentType);
        }
        private static Dictionary<string, string> contentTypes = new Dictionary<string, string>();
        private static void InitContentTypes()
        {
            contentTypes.Add(".323", "text/h323");
            contentTypes.Add(".3g2", "video/3gpp2");
            contentTypes.Add(".3gp2", "video/3gpp2");
            contentTypes.Add(".3gp", "video/3gpp");
            contentTypes.Add(".3gpp", "video/3gpp");
            contentTypes.Add(".aac", "audio/aac");
            contentTypes.Add(".aaf", "application/octet-stream");
            contentTypes.Add(".apk", "application/octet-stream");
            contentTypes.Add(".aca", "application/octet-stream");
            contentTypes.Add(".accdb", "application/msaccess");
            contentTypes.Add(".accde", "application/msaccess");
            contentTypes.Add(".accdt", "application/msaccess");
            contentTypes.Add(".acx", "application/internet-property-stream");
            contentTypes.Add(".adt", "audio/vnd.dlna.adts");
            contentTypes.Add(".adts", "audio/vnd.dlna.adts");
            contentTypes.Add(".afm", "application/octet-stream");
            contentTypes.Add(".ai", "application/postscript");
            contentTypes.Add(".aif", "audio/x-aiff");
            contentTypes.Add(".aifc", "audio/aiff");
            contentTypes.Add(".aiff", "audio/aiff");
            contentTypes.Add(".application", "application/x-ms-application");
            contentTypes.Add(".art", "image/x-jg");
            contentTypes.Add(".asd", "application/octet-stream");
            contentTypes.Add(".asf", "video/x-ms-asf");
            contentTypes.Add(".asi", "application/octet-stream");
            contentTypes.Add(".asm", "text/plain");
            contentTypes.Add(".asr", "video/x-ms-asf");
            contentTypes.Add(".asx", "video/x-ms-asf");
            contentTypes.Add(".atom", "application/atom+xml");
            contentTypes.Add(".au", "audio/basic");
            contentTypes.Add(".avi", "video/x-msvideo");
            contentTypes.Add(".axs", "application/olescript");
            contentTypes.Add(".bas", "text/plain");
            contentTypes.Add(".bcpio", "application/x-bcpio");
            contentTypes.Add(".bin", "application/octet-stream");
            contentTypes.Add(".bmp", "image/bmp");
            contentTypes.Add(".c", "text/plain");
            contentTypes.Add(".cab", "application/vnd.ms-cab-compressed");
            contentTypes.Add(".calx", "application/vnd.ms-office.calx");
            contentTypes.Add(".cat", "application/vnd.ms-pki.seccat");
            contentTypes.Add(".cdf", "application/x-cdf");
            contentTypes.Add(".chm", "application/octet-stream");
            contentTypes.Add(".class", "application/x-java-applet");
            contentTypes.Add(".clp", "application/x-msclip");
            contentTypes.Add(".cmx", "image/x-cmx");
            contentTypes.Add(".cnf", "text/plain");
            contentTypes.Add(".cod", "image/cis-cod");
            contentTypes.Add(".cpio", "application/x-cpio");
            contentTypes.Add(".cpp", "text/plain");
            contentTypes.Add(".crd", "application/x-mscardfile");
            contentTypes.Add(".crl", "application/pkix-crl");
            contentTypes.Add(".crt", "application/x-x509-ca-cert");
            contentTypes.Add(".csh", "application/x-csh");
            contentTypes.Add(".css", "text/css");
            contentTypes.Add(".csv", "text/csv");
            contentTypes.Add(".cur", "application/octet-stream");
            contentTypes.Add(".dcr", "application/x-director");
            contentTypes.Add(".deploy", "application/octet-stream");
            contentTypes.Add(".der", "application/x-x509-ca-cert");
            contentTypes.Add(".dib", "image/bmp");
            contentTypes.Add(".dir", "application/x-director");
            contentTypes.Add(".disco", "text/xml");
            contentTypes.Add(".dll", "application/x-msdownload");
            contentTypes.Add(".dll.config", "text/xml");
            contentTypes.Add(".dlm", "text/dlm");
            contentTypes.Add(".doc", "application/msword");
            contentTypes.Add(".docm", "application/vnd.ms-word.document.macroEnabled.12");
            contentTypes.Add(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            contentTypes.Add(".dot", "application/msword");
            contentTypes.Add(".dotm", "application/vnd.ms-word.template.macroEnabled.12");
            contentTypes.Add(".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template");
            contentTypes.Add(".dsp", "application/octet-stream");
            contentTypes.Add(".dtd", "text/xml");
            contentTypes.Add(".dvi", "application/x-dvi");
            contentTypes.Add(".dvr-ms", "video/x-ms-dvr");
            contentTypes.Add(".dwf", "drawing/x-dwf");
            contentTypes.Add(".dwp", "application/octet-stream");
            contentTypes.Add(".dxr", "application/x-director");
            contentTypes.Add(".eml", "message/rfc822");
            contentTypes.Add(".emz", "application/octet-stream");
            contentTypes.Add(".eot", "application/vnd.ms-fontobject");
            contentTypes.Add(".eps", "application/postscript");
            contentTypes.Add(".etx", "text/x-setext");
            contentTypes.Add(".evy", "application/envoy");
            contentTypes.Add(".exe", "application/octet-stream");
            contentTypes.Add(".exe.config", "text/xml");
            contentTypes.Add(".fdf", "application/vnd.fdf");
            contentTypes.Add(".fif", "application/fractals");
            contentTypes.Add(".fla", "application/octet-stream");
            contentTypes.Add(".flr", "x-world/x-vrml");
            contentTypes.Add(".flv", "video/x-flv");
            contentTypes.Add(".gif", "image/gif");
            contentTypes.Add(".gtar", "application/x-gtar");
            contentTypes.Add(".gz", "application/x-gzip");
            contentTypes.Add(".h", "text/plain");
            contentTypes.Add(".hdf", "application/x-hdf");
            contentTypes.Add(".hdml", "text/x-hdml");
            contentTypes.Add(".hhc", "application/x-oleobject");
            contentTypes.Add(".hhk", "application/octet-stream");
            contentTypes.Add(".hhp", "application/octet-stream");
            contentTypes.Add(".hlp", "application/winhlp");
            contentTypes.Add(".hqx", "application/mac-binhex40");
            contentTypes.Add(".hta", "application/hta");
            contentTypes.Add(".htc", "text/x-component");
            contentTypes.Add(".htm", "text/html");
            contentTypes.Add(".html", "text/html");
            contentTypes.Add(".htt", "text/webviewhtml");
            contentTypes.Add(".hxt", "text/html");
            contentTypes.Add(".ical", "text/calendar");
            contentTypes.Add(".icalendar", "text/calendar");
            contentTypes.Add(".ico", "image/x-icon");
            contentTypes.Add(".ics", "text/calendar");
            contentTypes.Add(".ief", "image/ief");
            contentTypes.Add(".ifb", "text/calendar");
            contentTypes.Add(".iii", "application/x-iphone");
            contentTypes.Add(".inf", "application/octet-stream");
            contentTypes.Add(".ins", "application/x-internet-signup");
            contentTypes.Add(".isp", "application/x-internet-signup");
            contentTypes.Add(".IVF", "video/x-ivf");
            contentTypes.Add(".jar", "application/java-archive");
            contentTypes.Add(".java", "application/octet-stream");
            contentTypes.Add(".jck", "application/liquidmotion");
            contentTypes.Add(".jcz", "application/liquidmotion");
            contentTypes.Add(".jfif", "image/pjpeg");
            contentTypes.Add(".jpb", "application/octet-stream");
            contentTypes.Add(".jpe", "image/jpeg");
            contentTypes.Add(".jpeg", "image/jpeg");
            contentTypes.Add(".jpg", "image/jpeg");
            contentTypes.Add(".js", "application/javascript");
            contentTypes.Add(".jsx", "text/jscript");
            contentTypes.Add(".latex", "application/x-latex");
            contentTypes.Add(".lit", "application/x-ms-reader");
            contentTypes.Add(".lpk", "application/octet-stream");
            contentTypes.Add(".lsf", "video/x-la-asf");
            contentTypes.Add(".lsx", "video/x-la-asf");
            contentTypes.Add(".lzh", "application/octet-stream");
            contentTypes.Add(".m13", "application/x-msmediaview");
            contentTypes.Add(".m14", "application/x-msmediaview");
            contentTypes.Add(".m1v", "video/mpeg");
            contentTypes.Add(".m2ts", "video/vnd.dlna.mpeg-tts");
            contentTypes.Add(".m3u", "audio/x-mpegurl");
            contentTypes.Add(".m4a", "audio/mp4");
            contentTypes.Add(".m4v", "video/mp4");
            contentTypes.Add(".man", "application/x-troff-man");
            contentTypes.Add(".manifest", "application/x-ms-manifest");
            contentTypes.Add(".map", "text/plain");
            contentTypes.Add(".mdb", "application/x-msaccess");
            contentTypes.Add(".mdp", "application/octet-stream");
            contentTypes.Add(".me", "application/x-troff-me");
            contentTypes.Add(".mht", "message/rfc822");
            contentTypes.Add(".mhtml", "message/rfc822");
            contentTypes.Add(".mid", "audio/mid");
            contentTypes.Add(".midi", "audio/mid");
            contentTypes.Add(".mix", "application/octet-stream");
            contentTypes.Add(".mmf", "application/x-smaf");
            contentTypes.Add(".mno", "text/xml");
            contentTypes.Add(".mny", "application/x-msmoney");
            contentTypes.Add(".mov", "video/quicktime");
            contentTypes.Add(".movie", "video/x-sgi-movie");
            contentTypes.Add(".mp2", "video/mpeg");
            contentTypes.Add(".mp3", "audio/mpeg");
            contentTypes.Add(".mp4", "video/mp4");
            contentTypes.Add(".mp4v", "video/mp4");
            contentTypes.Add(".mpa", "video/mpeg");
            contentTypes.Add(".mpe", "video/mpeg");
            contentTypes.Add(".mpeg", "video/mpeg");
            contentTypes.Add(".mpg", "video/mpeg");
            contentTypes.Add(".mpp", "application/vnd.ms-project");
            contentTypes.Add(".mpv2", "video/mpeg");
            contentTypes.Add(".ms", "application/x-troff-ms");
            contentTypes.Add(".msi", "application/octet-stream");
            contentTypes.Add(".mso", "application/octet-stream");
            contentTypes.Add(".mvb", "application/x-msmediaview");
            contentTypes.Add(".mvc", "application/x-miva-compiled");
            contentTypes.Add(".nc", "application/x-netcdf");
            contentTypes.Add(".nsc", "video/x-ms-asf");
            contentTypes.Add(".nws", "message/rfc822");
            contentTypes.Add(".ocx", "application/octet-stream");
            contentTypes.Add(".oda", "application/oda");
            contentTypes.Add(".odc", "text/x-ms-odc");
            contentTypes.Add(".ods", "application/oleobject");
            contentTypes.Add(".oga", "audio/ogg");
            contentTypes.Add(".ogg", "video/ogg");
            contentTypes.Add(".ogv", "video/ogg");
            contentTypes.Add(".ogx", "application/ogg");
            contentTypes.Add(".one", "application/onenote");
            contentTypes.Add(".onea", "application/onenote");
            contentTypes.Add(".onetoc", "application/onenote");
            contentTypes.Add(".onetoc2", "application/onenote");
            contentTypes.Add(".onetmp", "application/onenote");
            contentTypes.Add(".onepkg", "application/onenote");
            contentTypes.Add(".osdx", "application/opensearchdescription+xml");
            contentTypes.Add(".otf", "font/otf");
            contentTypes.Add(".p10", "application/pkcs10");
            contentTypes.Add(".p12", "application/x-pkcs12");
            contentTypes.Add(".p7b", "application/x-pkcs7-certificates");
            contentTypes.Add(".p7c", "application/pkcs7-mime");
            contentTypes.Add(".p7m", "application/pkcs7-mime");
            contentTypes.Add(".p7r", "application/x-pkcs7-certreqresp");
            contentTypes.Add(".p7s", "application/pkcs7-signature");
            contentTypes.Add(".pbm", "image/x-portable-bitmap");
            contentTypes.Add(".pcx", "application/octet-stream");
            contentTypes.Add(".pcz", "application/octet-stream");
            contentTypes.Add(".pdf", "application/pdf");
            contentTypes.Add(".pfb", "application/octet-stream");
            contentTypes.Add(".pfm", "application/octet-stream");
            contentTypes.Add(".pfx", "application/x-pkcs12");
            contentTypes.Add(".pgm", "image/x-portable-graymap");
            contentTypes.Add(".pko", "application/vnd.ms-pki.pko");
            contentTypes.Add(".pma", "application/x-perfmon");
            contentTypes.Add(".pmc", "application/x-perfmon");
            contentTypes.Add(".pml", "application/x-perfmon");
            contentTypes.Add(".pmr", "application/x-perfmon");
            contentTypes.Add(".pmw", "application/x-perfmon");
            contentTypes.Add(".png", "image/png");
            contentTypes.Add(".pnm", "image/x-portable-anymap");
            contentTypes.Add(".pnz", "image/png");
            contentTypes.Add(".pot", "application/vnd.ms-powerpoint");
            contentTypes.Add(".potm", "application/vnd.ms-powerpoint.template.macroEnabled.12");
            contentTypes.Add(".potx", "application/vnd.openxmlformats-officedocument.presentationml.template");
            contentTypes.Add(".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12");
            contentTypes.Add(".ppm", "image/x-portable-pixmap");
            contentTypes.Add(".pps", "application/vnd.ms-powerpoint");
            contentTypes.Add(".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12");
            contentTypes.Add(".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow");
            contentTypes.Add(".ppt", "application/vnd.ms-powerpoint");
            contentTypes.Add(".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12");
            contentTypes.Add(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
            contentTypes.Add(".prf", "application/pics-rules");
            contentTypes.Add(".prm", "application/octet-stream");
            contentTypes.Add(".prx", "application/octet-stream");
            contentTypes.Add(".ps", "application/postscript");
            contentTypes.Add(".psd", "application/octet-stream");
            contentTypes.Add(".psm", "application/octet-stream");
            contentTypes.Add(".psp", "application/octet-stream");
            contentTypes.Add(".pub", "application/x-mspublisher");
            contentTypes.Add(".qt", "video/quicktime");
            contentTypes.Add(".qtl", "application/x-quicktimeplayer");
            contentTypes.Add(".qxd", "application/octet-stream");
            contentTypes.Add(".ra", "audio/x-pn-realaudio");
            contentTypes.Add(".ram", "audio/x-pn-realaudio");
            contentTypes.Add(".rar", "application/octet-stream");
            contentTypes.Add(".ras", "image/x-cmu-raster");
            contentTypes.Add(".rf", "image/vnd.rn-realflash");
            contentTypes.Add(".rgb", "image/x-rgb");
            contentTypes.Add(".rm", "application/vnd.rn-realmedia");
            contentTypes.Add(".rmi", "audio/mid");
            contentTypes.Add(".roff", "application/x-troff");
            contentTypes.Add(".rpm", "audio/x-pn-realaudio-plugin");
            contentTypes.Add(".rtf", "application/rtf");
            contentTypes.Add(".rtx", "text/richtext");
            contentTypes.Add(".scd", "application/x-msschedule");
            contentTypes.Add(".sct", "text/scriptlet");
            contentTypes.Add(".sea", "application/octet-stream");
            contentTypes.Add(".setpay", "application/set-payment-initiation");
            contentTypes.Add(".setreg", "application/set-registration-initiation");
            contentTypes.Add(".sgml", "text/sgml");
            contentTypes.Add(".sh", "application/x-sh");
            contentTypes.Add(".shar", "application/x-shar");
            contentTypes.Add(".sit", "application/x-stuffit");
            contentTypes.Add(".sldm", "application/vnd.ms-powerpoint.slide.macroEnabled.12");
            contentTypes.Add(".sldx", "application/vnd.openxmlformats-officedocument.presentationml.slide");
            contentTypes.Add(".smd", "audio/x-smd");
            contentTypes.Add(".smi", "application/octet-stream");
            contentTypes.Add(".smx", "audio/x-smd");
            contentTypes.Add(".smz", "audio/x-smd");
            contentTypes.Add(".snd", "audio/basic");
            contentTypes.Add(".snp", "application/octet-stream");
            contentTypes.Add(".spc", "application/x-pkcs7-certificates");
            contentTypes.Add(".spl", "application/futuresplash");
            contentTypes.Add(".spx", "audio/ogg");
            contentTypes.Add(".src", "application/x-wais-source");
            contentTypes.Add(".ssm", "application/streamingmedia");
            contentTypes.Add(".sst", "application/vnd.ms-pki.certstore");
            contentTypes.Add(".stl", "application/vnd.ms-pki.stl");
            contentTypes.Add(".sv4cpio", "application/x-sv4cpio");
            contentTypes.Add(".sv4crc", "application/x-sv4crc");
            contentTypes.Add(".svg", "image/svg+xml");
            contentTypes.Add(".svgz", "image/svg+xml");
            contentTypes.Add(".swf", "application/x-shockwave-flash");
            contentTypes.Add(".t", "application/x-troff");
            contentTypes.Add(".tar", "application/x-tar");
            contentTypes.Add(".tcl", "application/x-tcl");
            contentTypes.Add(".tex", "application/x-tex");
            contentTypes.Add(".texi", "application/x-texinfo");
            contentTypes.Add(".texinfo", "application/x-texinfo");
            contentTypes.Add(".tgz", "application/x-compressed");
            contentTypes.Add(".thmx", "application/vnd.ms-officetheme");
            contentTypes.Add(".thn", "application/octet-stream");
            contentTypes.Add(".tif", "image/tiff");
            contentTypes.Add(".tiff", "image/tiff");
            contentTypes.Add(".toc", "application/octet-stream");
            contentTypes.Add(".crx", "application/x-chrome-extension");
            contentTypes.Add(".tr", "application/x-troff");
            contentTypes.Add(".trm", "application/x-msterminal");
            contentTypes.Add(".ts", "video/vnd.dlna.mpeg-tts");
            contentTypes.Add(".tsv", "text/tab-separated-values");
            contentTypes.Add(".ttf", "application/octet-stream");
            contentTypes.Add(".tts", "video/vnd.dlna.mpeg-tts");
            contentTypes.Add(".txt", "text/plain");
            contentTypes.Add(".u32", "application/octet-stream");
            contentTypes.Add(".uls", "text/iuls");
            contentTypes.Add(".ustar", "application/x-ustar");
            contentTypes.Add(".vbs", "text/vbscript");
            contentTypes.Add(".vcf", "text/x-vcard");
            contentTypes.Add(".vcs", "text/plain");
            contentTypes.Add(".vdx", "application/vnd.ms-visio.viewer");
            contentTypes.Add(".vml", "text/xml");
            contentTypes.Add(".vsd", "application/vnd.visio");
            contentTypes.Add(".vss", "application/vnd.visio");
            contentTypes.Add(".vst", "application/vnd.visio");
            contentTypes.Add(".vsto", "application/x-ms-vsto");
            contentTypes.Add(".vsw", "application/vnd.visio");
            contentTypes.Add(".vsx", "application/vnd.visio");
            contentTypes.Add(".vtx", "application/vnd.visio");
            contentTypes.Add(".wav", "audio/wav");
            contentTypes.Add(".wax", "audio/x-ms-wax");
            contentTypes.Add(".wbmp", "image/vnd.wap.wbmp");
            contentTypes.Add(".wcm", "application/vnd.ms-works");
            contentTypes.Add(".wdb", "application/vnd.ms-works");
            contentTypes.Add(".webm", "video/webm");
            contentTypes.Add(".wks", "application/vnd.ms-works");
            contentTypes.Add(".wm", "video/x-ms-wm");
            contentTypes.Add(".wma", "audio/x-ms-wma");
            contentTypes.Add(".wmd", "application/x-ms-wmd");
            contentTypes.Add(".wmf", "application/x-msmetafile");
            contentTypes.Add(".wml", "text/vnd.wap.wml");
            contentTypes.Add(".wmlc", "application/vnd.wap.wmlc");
            contentTypes.Add(".wmls", "text/vnd.wap.wmlscript");
            contentTypes.Add(".wmlsc", "application/vnd.wap.wmlscriptc");
            contentTypes.Add(".wmp", "video/x-ms-wmp");
            contentTypes.Add(".wmv", "video/x-ms-wmv");
            contentTypes.Add(".wmx", "video/x-ms-wmx");
            contentTypes.Add(".wmz", "application/x-ms-wmz");
            contentTypes.Add(".woff", "font/x-woff");
            contentTypes.Add(".wps", "application/vnd.ms-works");
            contentTypes.Add(".wri", "application/x-mswrite");
            contentTypes.Add(".wrl", "x-world/x-vrml");
            contentTypes.Add(".wrz", "x-world/x-vrml");
            contentTypes.Add(".wsdl", "text/xml");
            contentTypes.Add(".wtv", "video/x-ms-wtv");
            contentTypes.Add(".wvx", "video/x-ms-wvx");
            contentTypes.Add(".x", "application/directx");
            contentTypes.Add(".xaf", "x-world/x-vrml");
            contentTypes.Add(".xaml", "application/xaml+xml");
            contentTypes.Add(".xap", "application/x-silverlight-app");
            contentTypes.Add(".xbap", "application/x-ms-xbap");
            contentTypes.Add(".xbm", "image/x-xbitmap");
            contentTypes.Add(".xdr", "text/plain");
            contentTypes.Add(".xht", "application/xhtml+xml");
            contentTypes.Add(".xhtml", "application/xhtml+xml");
            contentTypes.Add(".xla", "application/vnd.ms-excel");
            contentTypes.Add(".xlam", "application/vnd.ms-excel.addin.macroEnabled.12");
            contentTypes.Add(".xlc", "application/vnd.ms-excel");
            contentTypes.Add(".xlm", "application/vnd.ms-excel");
            contentTypes.Add(".xls", "application/vnd.ms-excel");
            contentTypes.Add(".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12");
            contentTypes.Add(".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12");
            contentTypes.Add(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            contentTypes.Add(".xlt", "application/vnd.ms-excel");
            contentTypes.Add(".xltm", "application/vnd.ms-excel.template.macroEnabled.12");
            contentTypes.Add(".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template");
            contentTypes.Add(".xlw", "application/vnd.ms-excel");
            contentTypes.Add(".xml", "text/xml");
            contentTypes.Add(".json", "text/json");
            contentTypes.Add(".xof", "x-world/x-vrml");
            contentTypes.Add(".xpm", "image/x-xpixmap");
            contentTypes.Add(".xps", "application/vnd.ms-xpsdocument");
            contentTypes.Add(".xsd", "text/xml");
            contentTypes.Add(".xsf", "text/xml");
            contentTypes.Add(".xsl", "text/xml");
            contentTypes.Add(".xslt", "text/xml");
            contentTypes.Add(".xsn", "application/octet-stream");
            contentTypes.Add(".xtp", "application/octet-stream");
            contentTypes.Add(".xwd", "image/x-xwindowdump");
            contentTypes.Add(".z", "application/x-compress");
            contentTypes.Add(".zip", "application/x-zip-compressed");
            contentTypes.Add(".woff2", "font/x-woff");
        }
        static Data()
        {
            InitContentTypes();
        }
    }
}