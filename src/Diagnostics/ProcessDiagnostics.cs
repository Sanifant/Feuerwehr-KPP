using System.Diagnostics;

namespace De.Openelp.Feuerwehr.Diagnostics
{
    // Linked into the API and AppHost so each process records its own lifetime independently.
    /// <summary>
    /// Records process lifetime events and failures to console output and a rotating local diagnostic log.
    /// </summary>
    internal sealed class ProcessDiagnostics : IDisposable
    {
        private readonly object _sync = new ();
        private readonly string _name;
        private readonly string _path;

        private ProcessDiagnostics(string name)
        {
            this._name = name;
            this._path = Path.Combine(AppContext.BaseDirectory, "logs", "process-diagnostics.log");
            AppDomain.CurrentDomain.UnhandledException += this.Unhandled;
            AppDomain.CurrentDomain.ProcessExit += this.Exited;
            TaskScheduler.UnobservedTaskException += this.Unobserved;
            Console.CancelKeyPress += this.Canceled;
            this.Record("Process.Start", $"PID={Environment.ProcessId}; DebuggerAttached={Debugger.IsAttached}; Runtime={Environment.Version}");
            this.Record("Diagnostics.File", this._path);
        }

        /// <summary>
        /// Subscribes to process lifetime events and records startup diagnostics.
        /// </summary>
        /// <param name="name">The display name of the process diagnostics.</param>
        /// <returns>The diagnostics subscription to dispose when the process scope ends.</returns>
        public static ProcessDiagnostics Start(string name) => new (name);

        /// <summary>
        /// Writes a diagnostic event to console output and the rotating process log without propagating logging failures.
        /// </summary>
        /// <param name="step">The lifecycle step or diagnostic event name to record.</param>
        /// <param name="details">Optional context to include in the diagnostic entry.</param>
        /// <param name="exception">The failure to include in the diagnostic entry, when present.</param>
        public void Record(string step, string? details = null, Exception? exception = null)
        {
            string text = $"{DateTime.UtcNow:O} [{this._name}] {step}: {details}";
            if (exception is not null)
            {
                text += Environment.NewLine + exception;
            }

            try
            {
                Debug.WriteLine(text);
                if (exception is null)
                {
                    Console.WriteLine(text);
                }
                else
                {
                    Console.Error.WriteLine(text);
                }
            }
            catch (Exception)
            { /* Diagnostics must not terminate a process with a closed output stream. */
            }

            try
            {
                lock (this._sync)
                {
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(this._path)!);
                    if (File.Exists(this._path) && new FileInfo(this._path).Length > 2 * 1024 * 1024)
                    {
                        File.Move(this._path, this._path + ".previous", overwrite: true);
                    }

                    File.AppendAllText(this._path, text + Environment.NewLine);
                }
            }
            catch (Exception)
            { /* A read-only/full filesystem must not affect application behavior. */
            }
        }

        /// <summary>
        /// Records diagnostic shutdown and removes process lifetime event subscriptions.
        /// </summary>
        public void Dispose()
        {
            this.Record("Process.DiagnosticsDisposed", $"ExitCode={Environment.ExitCode}");
            AppDomain.CurrentDomain.UnhandledException -= this.Unhandled;
            AppDomain.CurrentDomain.ProcessExit -= this.Exited;
            TaskScheduler.UnobservedTaskException -= this.Unobserved;
            Console.CancelKeyPress -= this.Canceled;
        }

        private void Unhandled(object sender, UnhandledExceptionEventArgs args)
            => this.Record("Process.UnhandledException", $"IsTerminating={args.IsTerminating}", args.ExceptionObject as Exception);

        private void Unobserved(object? sender, UnobservedTaskExceptionEventArgs args)
            => this.Record("Process.UnobservedTaskException", exception: args.Exception);

        private void Exited(object? sender, EventArgs args)
            => this.Record("Process.Exit", $"ExitCode={Environment.ExitCode}");

        private void Canceled(object? sender, ConsoleCancelEventArgs args)
            => this.Record("Process.CancelKeyPress", $"Key={args.SpecialKey}");
    }
}
