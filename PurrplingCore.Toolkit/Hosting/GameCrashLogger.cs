namespace PurrplingCore.Toolkit.Hosting;

/// <summary>
/// Configuration for process-wide crash logging.
/// </summary>
public sealed class GameHostCrashLoggerOptions
{
    /// <summary>
    /// Gets or sets the file name used for a captured exception.
    /// Relative names are written to the application's base directory.
    /// </summary>
    public string LogFileName { get; init; } = GameCrashLogger.DefaultLogFileName;
    public string BaseDirectory { get; init; } = AppContext.BaseDirectory;
    public bool WriteCrashLog { get; init; } = HostEnvironment.IsReleaseBuild;
}

/// <summary>
/// Installs process-wide logging for unhandled application and task exceptions.
/// </summary>
public static class GameCrashLogger
{
    public const string DefaultLogFileName = "crash.log";

    private static readonly object RegistrationLock = new();
    private static string? _logFilePath;
    private static string? _logFileName;
    private static bool _writeCrashLogFile = HostEnvironment.IsReleaseBuild;

    /// <summary>
    /// Gets whether the process-wide crash logger has already been registered.
    /// </summary>
    public static bool IsRegistered
    {
        get => Volatile.Read(ref _logFilePath) != null;
    }

    /// <summary>
    /// Registers the logger using the current application's base directory.
    /// </summary>
    public static void Register(GameHostCrashLoggerOptions? options = null)
    {
        options ??= new GameHostCrashLoggerOptions();
        _writeCrashLogFile = options.WriteCrashLog;

        ArgumentException.ThrowIfNullOrWhiteSpace(options.LogFileName);
        var logFilePath = GetLogFilePath(options.BaseDirectory, options.LogFileName);

        lock (RegistrationLock)
        {
            if (_logFilePath != null)
            {
                throw new Exception("The crash logger has already been registered.");
            }

            _logFileName = options.LogFileName;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            Volatile.Write(ref _logFilePath, logFilePath);
        }
    }

    public static void Unregister()
    {
        lock (RegistrationLock)
        {
            if (_logFilePath == null)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            Volatile.Write(ref _logFilePath, null);
            _logFileName = null;
            _writeCrashLogFile = HostEnvironment.IsReleaseBuild;
        }
    }

    private static void OnUnhandledException(object? sender, UnhandledExceptionEventArgs args)
    {
        if (_writeCrashLogFile && args.ExceptionObject is Exception exception)
        {
            Write(exception);
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        if (_writeCrashLogFile)
        {
            Write(args.Exception);
        }
    }

    private static void Write(Exception exception)
    {
        var logFileName = Volatile.Read(ref _logFileName) ?? DefaultLogFileName;
        var fallbackLogFile = GetLogFilePath(AppContext.BaseDirectory, logFileName);
        var logFile = _logFilePath ?? fallbackLogFile;

        TryWrite(logFile, exception);

        if (!string.Equals(logFile, fallbackLogFile, StringComparison.OrdinalIgnoreCase))
        {
            TryWrite(fallbackLogFile, exception);
        }
    }

    private static void TryWrite(string logFilePath, Exception exception)
    {
        try
        {
            File.WriteAllText(logFilePath, exception.ToString());
        }
        catch
        {
            // Crash logging must never conceal the original failure.
        }
    }

    private static string GetLogFilePath(string baseDirectory, string logFileName)
    {
        var path = Path.IsPathRooted(logFileName)
            ? logFileName
            : Path.Combine(baseDirectory, logFileName);

        return Path.GetFullPath(path);
    }
}
