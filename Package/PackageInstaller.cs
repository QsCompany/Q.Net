using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.Package
{

    public delegate void AppsChangedHandler(PackageInstaller manager, AppInfo added, AppInfo removed);
    public delegate void CurrentAPPHandler(Desktop manager, AppInfo _old, AppInfo _new);

    public class PackageInstaller
    {
        public Desktop Desktop { get; }
        public event AppsChangedHandler OnAppInstalledOrUninstalled;

        public PackageInstaller(Desktop desktop)
        {
            Desktop = desktop;
        }
        #region Package-Init
        private ZipFile Initialize(ZipFile zip = null)
        {
            zip = zip ?? new ZipFile();

            zip.CompressionLevel = Ionic.Zlib.CompressionLevel.BestCompression;
            //CompressionMethod = CompressionMethod.BZip2,
            zip.Password = "AchourBrahimIsWatchingYou";
            //zip.Strategy = Ionic.Zlib.CompressionStrategy.HuffmanOnly;
            zip.Encryption = EncryptionAlgorithm.WinZipAes256;
            //zip.FlattenFoldersOnExtract = true;
            zip.SortEntriesBeforeSaving = true;

            return zip;
        }
        #endregion
        #region Package-Install
        public bool Install(AppInfo app, ExtractExistingFileAction action = ExtractExistingFileAction.DoNotOverwrite)
        {
            if (string.IsNullOrWhiteSpace(app.InstalationPath)) app.SetInstalationPath($@"Data\{app.AppName}\");
            if (!app.ResourcePath.Exists) app.ResourcePath.Create();
            using (var zip = Initialize(ZipFile.Read(app.AppPath.FullName)))
                zip.ExtractAll(app.ResourcePath.FullName, action);
            app.GenerateManifest();
            Desktop.Apps.Add(app);
            Desktop.Apps.Save();
            OnAppInstalledOrUninstalled?.Invoke(this, app, null);
            return true;
        }
        #endregion

        #region Package-Uninstall
        public bool Uninstall(AppInfo app)
        {
            if (app.ResourcePath.Exists)
                app.ResourcePath.Delete(true);
            Desktop.Apps.Delete(app);
            Desktop.Apps.Save();
            OnAppInstalledOrUninstalled?.Invoke(this, null, app);
            return true;
        }

        #endregion
    }
}
