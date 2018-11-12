using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Utils
{

    public static class Help
    {
        public static long ToJSDate(this DateTime t) => (t.Ticks - 621355968000000000) / 10000;
        public static DateTime FromJSDate(this long t) => new DateTime((t * 10000 + 621355968000000000));

        public static string GetRelativePath(this string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);
            Uri folderUri = new Uri(folder.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()) ? folder : folder + System.IO.Path.DirectorySeparatorChar);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', System.IO.Path.DirectorySeparatorChar));
        }
    }
}
