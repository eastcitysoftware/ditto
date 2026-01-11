namespace Ditto.Cli.Tests;

public class FileWatcherTests {
    [Fact]
    public void FileWatcher_ThrowsException_WhenDirectoryDoesNotExist() {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var extensions = new[] { ".txt" };

        var exception = Assert.Throws<DirectoryNotFoundException>(() => new FileWatcher(nonExistentDir, extensions));
        Assert.Contains("Watch directory does not exist", exception.Message);
    }

    [Fact]
    public void FileWatcher_InitializesCorrectly_WhenDirectoryExists() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var extensions = new[] { ".txt" };

        using var fileWatcher = new FileWatcher(tempDir, extensions);

        Assert.NotNull(fileWatcher);
        Directory.Delete(tempDir);
    }

    [Fact]
    public async Task FileWatcher_StartsAndStopsWatching() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var extensions = new[] { ".txt" };

        using var fileWatcher = new FileWatcher(tempDir, extensions);
        var isRunning = true;
        var watchTask = Task.Run(() => fileWatcher.Start(() => isRunning));

        // Let it run for a short time (longer than the idle interval to test interval adjustment)
        await Task.Delay(3000);
        isRunning = false; // Signal to stop watching
        await watchTask;

        Assert.True(watchTask.IsCompleted);
        Directory.Delete(tempDir);
    }

    [Fact]
    public async Task FileWatcher_DetectsFileChanges() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var extensions = new[] { ".txt" };
        var filePath = Path.Combine(tempDir, "test.txt");
        await File.WriteAllTextAsync(filePath, "Initial content");

        using var fileWatcher = new FileWatcher(tempDir, extensions);
        var changeDetected = false;

        fileWatcher.OnChangedAsync += async fileInfo => {
            changeDetected = true;
            await Task.CompletedTask;
        };

        var isRunning = true;
        var watchTask = Task.Run(() => fileWatcher.Start(() => isRunning));

        // Modify the file to trigger change detection
        await Task.Delay(1000); // Ensure watcher is started
        await File.WriteAllTextAsync(filePath, "Modified content");

        // Wait to allow change detection
        await Task.Delay(2000);
        isRunning = false; // Signal to stop watching
        await watchTask;

        Assert.True(changeDetected);
        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void FileWatcher_DisposesCorrectly() {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var extensions = new[] { ".txt" };

        var fileWatcher = new FileWatcher(tempDir, extensions);
        fileWatcher.Dispose();

        // If Dispose is called without exceptions, the test passes
        Assert.True(true);
        Directory.Delete(tempDir);
    }
}
