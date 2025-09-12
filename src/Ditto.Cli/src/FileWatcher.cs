using System.Collections.Concurrent;

namespace Ditto.Cli;

public sealed class WebsiteFileWatcher(string basePath) : IDisposable {
    private readonly ConcurrentDictionary<string, DateTime> _eventTimes = new();
    private readonly TimeSpan _debounceTime = TimeSpan.FromMilliseconds(200);

    private readonly FileSystemWatcher _fileSystemWatcher = new(basePath) {
        EnableRaisingEvents = true,
        IncludeSubdirectories = true,
        NotifyFilter = NotifyFilters.FileName
                       | NotifyFilters.DirectoryName
                       | NotifyFilters.LastWrite
                       | NotifyFilters.Size
    };

    public event Func<string, Task>? OnChangedAsync;

    public void Start(Func<bool> checkContinue) {
        _fileSystemWatcher.Filters.Add("*.toml");
        _fileSystemWatcher.Filters.Add("*.html");
        _fileSystemWatcher.Filters.Add("*.md");
        _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
        _fileSystemWatcher.Created += FileSystemWatcher_Changed;
        _fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
        _fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;

        var shouldContinue = true;
        do {
            shouldContinue = checkContinue();
            Task.Delay(500).Wait();
        } while (shouldContinue);
    }

    public void Dispose() {
        _fileSystemWatcher.EnableRaisingEvents = false;
        _fileSystemWatcher.Dispose();
    }

    private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e) {
        if (OnChangedAsync != null) {
            var relativePath = Path.GetRelativePath(basePath, e.FullPath);
            _ = DebouncedOnChanged(relativePath);
        }
    }

    private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e) {
        if (OnChangedAsync != null) {
            var relativePath = Path.GetRelativePath(basePath, e.FullPath);
            _ = DebouncedOnChanged(relativePath);
        }
    }

    private async Task DebouncedOnChanged(string relativePath) {
        if (OnChangedAsync is not null) {
            var now = DateTime.UtcNow;
            var isFirst = !_eventTimes.ContainsKey(relativePath);

            if (isFirst) {
                _eventTimes[relativePath] = now;
                await OnChangedAsync.Invoke(relativePath);
            }
            else {
                var lastEventTime = _eventTimes[relativePath];
                if ((now - lastEventTime) > _debounceTime) {
                    _eventTimes[relativePath] = now;
                    await OnChangedAsync.Invoke(relativePath);
                }
            }
        }
    }
}
