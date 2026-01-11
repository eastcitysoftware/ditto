using System.Collections.Concurrent;

namespace Ditto.Cli;

public sealed class FileWatcher : IDisposable {
    private readonly string _watchDir;
    private readonly string[] _extensions;
    // private DateTime _lastChangeTime = DateTime.UtcNow;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, FileInfo> _fileStates = new();
    private readonly TimeSpan _defaultInterval = TimeSpan.FromMilliseconds(500);
    // private readonly TimeSpan _idleInterval = TimeSpan.FromSeconds(2);
    private TimeSpan _pollInterval;


    public delegate Task OnChangeFunc(FileInfo fileInfo);
    public event OnChangeFunc? OnChangedAsync;

    public FileWatcher(string watchDir, string[] extensions) {
        if (!Directory.Exists(watchDir)) {
            throw new DirectoryNotFoundException($"Watch directory does not exist: {watchDir}");
        }

        _watchDir = watchDir;
        _extensions = extensions;
        _pollInterval = _defaultInterval;
    }

    public void Start(Func<bool> checkContinue) {
        try {
            while (!_cts.Token.IsCancellationRequested && checkContinue()) {
                var files = GetWatchFiles(_watchDir, _extensions);

                foreach (var file in files) {
                    var fileInfo = GetFileInfo(file);

                    if (!_fileStates.TryGetValue(file, out var previousState)) {
                        // New file detected
                        _fileStates[file] = fileInfo;
                    }
                    else if (previousState.ModTime != fileInfo.ModTime || previousState.Size != fileInfo.Size) {
                        // File modified
                        _fileStates[file] = fileInfo;
                        OnChangedAsync?.Invoke(fileInfo).Wait(); // Block until the event handler completes
                    }
                }

                // // Adjust polling interval based on activity
                // if ((DateTime.UtcNow - _lastChangeTime) > _idleInterval) {
                //     _pollInterval = _idleInterval;
                // }
                // else {
                //     _pollInterval = _defaultInterval;
                // }

                Task.Delay(_pollInterval, _cts.Token).Wait(); // Block for the polling interval
            }
        }
        catch (TaskCanceledException) {
            // Gracefully handle task cancellation
        }
        catch (Exception ex) {
            Print.Error($"Polling task encountered an exception: {ex}");
            throw;
        }
    }

    public void Dispose() {
        _cts.Cancel();
        _cts.Dispose();
    }

    private static IEnumerable<string> GetWatchFiles(string watchDir, string[] extensions) {
        return Directory.EnumerateFiles(watchDir, "*.*", SearchOption.AllDirectories)
            .Where(file => extensions.Length == 0 || extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase));
    }

    private static FileInfo GetFileInfo(string filePath) {
        var fileInfo = new System.IO.FileInfo(filePath);
        return new FileInfo {
            Path = filePath,
            Size = fileInfo.Length,
            ModTime = fileInfo.LastWriteTimeUtc
        };
    }

    public class FileInfo {
        public string Path { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime ModTime { get; set; }
    }
}

// public sealed class WebsiteFileWatcher(string basePath) : IDisposable {
//     private readonly ConcurrentDictionary<string, DateTime> _eventTimes = new();
//     private static readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(200);
//     private static readonly SemaphoreSlim _fileLock = new SemaphoreSlim(1, 1);

//     private readonly FileSystemWatcher _fileSystemWatcher = new(basePath) {
//         EnableRaisingEvents = true,
//         IncludeSubdirectories = true,
//         NotifyFilter = NotifyFilters.FileName
//                        | NotifyFilters.DirectoryName
//                        | NotifyFilters.LastWrite
//                        | NotifyFilters.Size
//     };

//     public event Func<string, Task>? OnChangedAsync;

//     public void Start(Func<bool> checkContinue) {
//         _fileSystemWatcher.Filters.Add("*.toml");
//         _fileSystemWatcher.Filters.Add("*.html");
//         _fileSystemWatcher.Filters.Add("*.md");
//         _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
//         _fileSystemWatcher.Created += FileSystemWatcher_Changed;
//         _fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
//         _fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;

//         var shouldContinue = true;
//         do {
//             shouldContinue = checkContinue();
//             Task.Delay(500).Wait();
//         } while (shouldContinue);
//     }

//     public void Dispose() {
//         _fileSystemWatcher.EnableRaisingEvents = false;
//         _fileSystemWatcher.Dispose();
//     }

//     private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
//         if (OnChangedAsync != null) {
//             var relativePath = Path.GetRelativePath(basePath, e.FullPath);
//             _ = DebouncedOnChanged(relativePath);
//         }
//     }

//     private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e) {
//         if (OnChangedAsync != null) {
//             var relativePath = Path.GetRelativePath(basePath, e.FullPath);
//             _ = DebouncedOnChanged(relativePath);
//         }
//     }

//     private async Task DebouncedOnChanged(string relativePath) {
//         await _fileLock.WaitAsync();
//         try {
//             if (OnChangedAsync is not null) {
//                 var now = DateTime.UtcNow;
//                 var isFirst = !_eventTimes.ContainsKey(relativePath);

//                 if (isFirst) {
//                     _eventTimes[relativePath] = now;
//                     await OnChangedAsync.Invoke(relativePath);
//                 }
//                 else {
//                     var lastEventTime = _eventTimes[relativePath];
//                     if ((now - lastEventTime) > _debounceTime) {
//                         _eventTimes[relativePath] = now;
//                         await OnChangedAsync.Invoke(relativePath);
//                     }
//                 }
//             }
//         }
//         finally {
//             _fileLock.Release();
//         }
//     }
// }
