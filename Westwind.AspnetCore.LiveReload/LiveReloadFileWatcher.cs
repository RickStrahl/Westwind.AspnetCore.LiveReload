using System.IO;
using Microsoft.Extensions.Options;

namespace Westwind.AspNetCore.LiveReload
{
    public static class LiveReloadFileWatcher
    {

        private static System.IO.FileSystemWatcher Watcher;
        private static IOptionsMonitor<LiveReloadConfiguration> Config = null;

        public static void StartFileWatcher(IOptionsMonitor<LiveReloadConfiguration> config)
        {
            // If this is a first start, initialize the config monitoring
            if (Config is null)
            {
                Config = config;
                Config.OnChange(OnConfigChanged);
            }

            if (!Config.CurrentValue.LiveReloadEnabled)
                return;

           var path = Config.CurrentValue.FolderToMonitor;
            path = Path.GetFullPath(path);

            Watcher = new FileSystemWatcher(path);
            Watcher.Filter = "*.*";
            Watcher.EnableRaisingEvents = true;
            Watcher.IncludeSubdirectories = true;

            Watcher.NotifyFilter = NotifyFilters.LastWrite
                                   | NotifyFilters.FileName
                                   | NotifyFilters.DirectoryName;



            Watcher.Changed += Watcher_Changed;
            Watcher.Created += Watcher_Changed;
            Watcher.Renamed += Watcher_Renamed;
        }

        private static void OnConfigChanged(LiveReloadConfiguration config, string configName)
        {
            if (config.LiveReloadEnabled)
            {
                StartFileWatcher(Config);
            }
            else
            {
                StopFileWatcher();
            }
        }

        public static void StopFileWatcher()
        {
            Watcher?.Dispose();
            Watcher = null;
        }

        private static void FileChanged(string filename)
        {
            if (filename.Contains("\\node_modules\\"))
                return;

            if (string.IsNullOrEmpty(filename) ||
                !Config.CurrentValue.LiveReloadEnabled)
                return;

            var ext = Path.GetExtension(filename);
            if (ext == null)
                return;

            if (Config.CurrentValue.ClientFileExtensions.Contains(ext))
            {
                _ = LiveReloadMiddleware.RefreshWebSocketRequest();
            }

        }

        private static void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            FileChanged(e.FullPath);
        }


        private static void Watcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            FileChanged(e.FullPath);
        }
    }
}






