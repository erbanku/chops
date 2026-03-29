namespace Chops.Services;

/// <summary>
/// Watches skill directories for filesystem changes using <see cref="FileSystemWatcher"/>.
/// Mirrors <c>FileWatcher.swift</c> (FSEvents-based) from the macOS version.
/// </summary>
public sealed class FileWatcher : IDisposable
{
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Action<string> _callback;
    private CancellationTokenSource? _debounceCts;
    private readonly TimeSpan _debounceDelay = TimeSpan.FromMilliseconds(500);

    public FileWatcher(Action<string> callback)
    {
        _callback = callback;
    }

    /// <summary>
    /// Begins watching the specified directories for file changes.
    /// Any existing watchers are stopped first.
    /// </summary>
    public void WatchDirectories(IEnumerable<string> paths)
    {
        StopAll();

        foreach (var path in paths)
        {
            if (Directory.Exists(path))
                WatchDirectory(path);
        }
    }

    private void WatchDirectory(string path)
    {
        var watcher = new FileSystemWatcher(path)
        {
            NotifyFilter = NotifyFilters.FileName
                         | NotifyFilters.DirectoryName
                         | NotifyFilters.LastWrite
                         | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Changed += OnChanged;
        watcher.Created += OnChanged;
        watcher.Deleted += OnChanged;
        watcher.Renamed += OnRenamed;

        _watchers.Add(watcher);
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => DebouncedCallback(e.FullPath);

    private void OnRenamed(object sender, RenamedEventArgs e) => DebouncedCallback(e.FullPath);

    /// <summary>
    /// Debounces rapid filesystem events to avoid redundant scans.
    /// Waits 500ms after the last event before invoking the callback.
    /// </summary>
    private void DebouncedCallback(string path)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(_debounceDelay, token);
                if (!token.IsCancellationRequested)
                    _callback(path);
            }
            catch (TaskCanceledException)
            {
                // Debounce cancelled by a newer event — expected behavior
            }
        }, token);
    }

    /// <summary>
    /// Stops all active filesystem watchers and releases resources.
    /// </summary>
    public void StopAll()
    {
        _debounceCts?.Cancel();
        foreach (var watcher in _watchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _watchers.Clear();
    }

    public void Dispose() => StopAll();
}
