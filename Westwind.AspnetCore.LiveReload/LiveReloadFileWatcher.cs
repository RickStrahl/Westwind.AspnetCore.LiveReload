using System.IO;

namespace Westwind.AspNetCore.LiveReload
{
    public class LiveReloadFileWatcher
    {

        private static System.IO.FileSystemWatcher Watcher;


        public static void StartFileWatcher()
        {
            var path = LiveReloadConfiguration.Current.FolderToMonitor;
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

        public void StopFileWatcher()
        {
            Watcher?.Dispose();
        }

        private static void FileChanged(string filename)
        {
            if (!LiveReloadConfiguration.Current.LiveReloadEnabled ||
                filename.Contains("\\node_modules\\"))
                return;

            if (string.IsNullOrEmpty(filename) ||
                !LiveReloadConfiguration.Current.LiveReloadEnabled)
                return;

            var ext = Path.GetExtension(filename);
            if (ext == null)
                return;

            if (LiveReloadConfiguration.Current.ClientFileExtensions.Contains(ext))
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






