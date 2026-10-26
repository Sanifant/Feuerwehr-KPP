using System.Diagnostics;

namespace de.openelp.feuerwehr.Diagnostics;

// Linked into the API and AppHost so each process records its own lifetime independently.
internal sealed class ProcessDiagnostics : IDisposable
{
    private readonly object _sync = new();
    private readonly string _name;
    private readonly string _path;
    private ProcessDiagnostics(string name)
    {
        _name = name;
        _path = Path.Combine(AppContext.BaseDirectory, "logs", "process-diagnostics.log");
        AppDomain.CurrentDomain.UnhandledException += Unhandled;
        AppDomain.CurrentDomain.ProcessExit += Exited;
        TaskScheduler.UnobservedTaskException += Unobserved;
        Console.CancelKeyPress += Canceled;
        Record("Process.Start", $"PID={Environment.ProcessId}; DebuggerAttached={Debugger.IsAttached}; Runtime={Environment.Version}");
        Record("Diagnostics.File", _path);
    }

    public static ProcessDiagnostics Start(string name) => new(name);

    public void Record(string step, string? details = null, Exception? exception = null)
    {
        var text = $"{DateTime.UtcNow:O} [{_name}] {step}: {details}";
        if (exception is not null) text += Environment.NewLine + exception;
        try
        {
            Debug.WriteLine(text);
            if (exception is null) Console.WriteLine(text);
            else Console.Error.WriteLine(text);
        }
        catch (Exception) { /* Diagnostics must not terminate a process with a closed output stream. */ }
        try
        {
            lock (_sync)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
                if (File.Exists(_path) && new FileInfo(_path).Length > 2 * 1024 * 1024)
                    File.Move(_path, _path + ".previous", overwrite: true);
                File.AppendAllText(_path, text + Environment.NewLine);
            }
        }
        catch (Exception) { /* A read-only/full filesystem must not affect application behavior. */ }
    }

    private void Unhandled(object sender, UnhandledExceptionEventArgs args)
        => Record("Process.UnhandledException", $"IsTerminating={args.IsTerminating}", args.ExceptionObject as Exception);
    private void Unobserved(object? sender, UnobservedTaskExceptionEventArgs args)
        => Record("Process.UnobservedTaskException", exception: args.Exception);
    private void Exited(object? sender, EventArgs args)
        => Record("Process.Exit", $"ExitCode={Environment.ExitCode}");
    private void Canceled(object? sender, ConsoleCancelEventArgs args)
        => Record("Process.CancelKeyPress", $"Key={args.SpecialKey}");

    public void Dispose()
    {
        Record("Process.DiagnosticsDisposed", $"ExitCode={Environment.ExitCode}");
        AppDomain.CurrentDomain.UnhandledException -= Unhandled;
        AppDomain.CurrentDomain.ProcessExit -= Exited;
        TaskScheduler.UnobservedTaskException -= Unobserved;
        Console.CancelKeyPress -= Canceled;
    }
}
