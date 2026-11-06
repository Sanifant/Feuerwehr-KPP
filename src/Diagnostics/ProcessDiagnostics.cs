using System.Diagnostics;

namespace De.Openelp.Feuerwehr.Diagnostics
{
    // Linked into the API and AppHost so each process records its own lifetime independently.
    internal sealed class ProcessDiagnostics : IDisposable
    {
        private readonly object _sync = new();
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
        /// Executes start.
        /// </summary>
        public static ProcessDiagnostics Start(string name) => new(name);

        /// <summary>
        /// Executes record.
        /// </summary>
        /// <param name="step">The step.</param>
        /// <param name="details">The details.</param>
        /// <param name="exception">The exception.</param>
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
        /// Executes dispose.
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
