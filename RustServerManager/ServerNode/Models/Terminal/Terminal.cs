using Pty.Net;
using ServerNode.Logging;
using ServerNode.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ServerNode.Models.Terminal
{
    /// <summary>
    /// Disposable Terminal Object, preferrably called within a using statement.
    /// </summary>
    public abstract class Terminal : IDisposable, ITerminal
    {
        protected CancellationTokenSource CancellationTokenSource;
        protected CancellationToken CancellationToken;
        protected UTF8Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        protected bool _hasFinished = false;
        protected int _timeoutOnAwaitingInput;
        protected Task ReadyForInputTimeoutTsk;
        protected CancellationTokenSource ReadyForInputTimeoutTskCts;
        protected bool disposedValue;

        /// <summary>
        /// Event Handler raised when Terminal finishes it's procedure.
        /// </summary>
        public event EventHandler Finished;

        public string[] DeterminesInput { get; set; } = new string[] { };

        public TaskCompletionSource<object?> ReadyForInputTsk { get; set; }

        /// <summary>
        /// OS Native PseudoTerminal Object
        /// </summary>
        protected IPtyConnection PseudoTerminal { get; set; }

        /// <summary>
        /// The executable path for Terminal in Windows
        /// </summary>
        public virtual string ExecutablePath { get; set; }

        /// <summary>
        /// Check if the Terminal Executable exists
        /// </summary>
        /// <returns></returns>
        protected bool ExecutableExists()
        {
            // Check if the variable file exists, depending in windows/linux
            if (File.Exists(ExecutablePath))
            {
                // File Found
                return true;
            }
            else
            {
                // File not found
                return false;
            }
        }

        /// <summary>
        /// Whether the Terminal Procedure Finished
        /// </summary>
        protected bool HasFinished
        {
            get => _hasFinished;
            set
            {
                if (value == true)
                {
                    _hasFinished = value;
                    OnFinished();
                }
            }
        }

        /// <summary>
        /// Event Trigger for when the Terminal procedure finishes
        /// </summary>
        protected virtual void OnFinished()
        {
            Finished?.Invoke(this, null);
        }

        /// <summary>
        /// Create a Terminal Object with a default input timeout of 30 seconds
        /// </summary>
        /// <param name="timeoutOnAwaitingInput">seconds to wait for input whilst in awaiting input state</param>
        protected Terminal(int? timeoutOnAwaitingInputMilliseconds = null)
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            if (timeoutOnAwaitingInputMilliseconds != null)
            {
                _timeoutOnAwaitingInput = (int)timeoutOnAwaitingInputMilliseconds;
            }
        }

        /// <summary>
        /// Instantiate a new terminal containing Terminal natively, and return upon input available
        /// </summary>
        /// <param name="timeoutOnAwaitingInputMilliseconds"></param>
        /// <returns></returns>
        public static async Task<ITerminal> Instantiate<T>(TerminalStartUpOptions terminalStartUpOptions)
        {
            ITerminal _terminal = (ITerminal)Activator.CreateInstance(typeof(T), terminalStartUpOptions.InputTimeout);

            try
            {
                await _terminal.ConnectToTerminal(terminalStartUpOptions.Name);

                Log.Verbose("Terminal Connected");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not start the terminal", ex);
            }

            await _terminal.SendCommand($"\"{_terminal.ExecutablePath}\"");

            Log.Verbose("Terminal Waiting For Input");

            await _terminal.ReadyForInputTsk.Task;

            Log.Verbose("Terminal Ready");

            return _terminal;
        }

        /// <summary>
        /// Instantiate a new terminal containing Terminal natively, and return upon input available
        /// </summary>
        /// <param name="timeoutOnAwaitingInputMilliseconds"></param>
        /// <returns></returns>
        protected async Task InstantiateTerminal(TerminalStartUpOptions terminalStartUpOptions, string workingDir, string startup)
        {
            await ConnectToTerminal(terminalStartUpOptions.Name, workingDir);

            Log.Verbose("Terminal Connected");

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                await SendCommand($"cmd /c {startup}");
            }
            else if (Utility.OperatingSystemHelper.IsLinux())
            {
                await SendCommand($"bash -c {startup}");
            }
            else
            {
                throw new ApplicationException("Failed to determine operating system for instantiating terminal.");
            }

            await ReadyForInputTsk.Task;

            Log.Verbose("Terminal Ready");
        }

        /// <summary>
        /// Instantiate a new terminal containing Terminal natively, and return upon input available
        /// </summary>
        /// <param name="timeoutOnAwaitingInputMilliseconds"></param>
        /// <returns></returns>
        protected async Task InstantiateTerminal(TerminalStartUpOptions terminalStartUpOptions, string workingDir, string startup, string[] commandline)
        {
            await ConnectToTerminal(terminalStartUpOptions.Name, workingDir, startup, commandline);

            Log.Verbose("Terminal Connected");

            await ReadyForInputTsk.Task;

            Log.Verbose("Terminal Ready");
        }

        /// <summary>
        /// Cancel the input timeout task if it's not null
        /// </summary>
        protected void CancelInputTimeout()
        {
            ReadyForInputTimeoutTskCts?.Cancel();
            ReadyForInputTimeoutTsk = null;
        }

        /// <summary>
        /// Create new input timeout task
        /// </summary>
        protected void ApplyInputTimeout()
        {
            if (_timeoutOnAwaitingInput != 0)
            {
                if (ReadyForInputTimeoutTsk != null)
                {
                    ReadyForInputTimeoutTskCts.Cancel();
                }

                ReadyForInputTimeoutTskCts = new CancellationTokenSource();
                ReadyForInputTimeoutTsk = Task.Run(async () =>
                {
                    CancellationTokenSource localToken = ReadyForInputTimeoutTskCts;
                    await Task.Delay(_timeoutOnAwaitingInput);
                    if (!localToken.IsCancellationRequested && !HasFinished && CancellationTokenSource != null && !CancellationTokenSource.IsCancellationRequested)
                    {
                        Log.Warning($"Exceeded Awaiting Input Timeout: {_timeoutOnAwaitingInput}ms");
                        CancellationTokenSource.Cancel();
                    }
                }, ReadyForInputTimeoutTskCts.Token);
            }
        }

        /// <summary>
        /// Spawn a new Pseudoterminal, begin asyncronously reading its input and output, apply terminal events
        /// </summary>
        /// <returns></returns>
        public async Task ConnectToTerminal(string _terminalName, string workingDir = null, string executablePath = null, string[] commandLine = null)
        {
            string app = executablePath ?? (Utility.OperatingSystemHelper.IsWindows() ? Path.Combine(Environment.SystemDirectory, "cmd.exe") : "sh");
            PtyOptions options = new PtyOptions
            {
                Name = _terminalName,
                // TODO this should be quite long, and cover anything that Terminal can spit out in a single line + the current directory length
                Cols = Environment.CurrentDirectory.Length + app.Length + 100,
                // we want it line by line, no more than that
                Rows = 1,
                Cwd = workingDir ?? Program.WorkingDirectory,
                App = app,
                CommandLine = commandLine ?? new string[] { },
                VerbatimCommandLine = true
            };

            PseudoTerminal = await PtyProvider.SpawnAsync(options, this.CancellationToken);

            Log.Verbose($"Terminal Instantiated : {PseudoTerminal != null}");

            var processExitedTcs = new TaskCompletionSource<uint>();
            PseudoTerminal.ProcessExited += (sender, e) =>
            {
                PseudoTerminal.ProcessExited += delegate { Log.Verbose("base event, process exited"); };

                HasFinished = true;

                processExitedTcs.TrySetResult((uint)PseudoTerminal.ExitCode);

                using (this.CancellationToken.Register(() => processExitedTcs.TrySetCanceled(this.CancellationToken)))
                {
                    uint exitCode = (uint)PseudoTerminal.ExitCode;
                }
            };

            string GetTerminalExitCode() =>
                processExitedTcs.Task.IsCompleted ? $". Terminal process has exited with exit code {processExitedTcs.Task.GetAwaiter().GetResult()}." : string.Empty;

            TaskCompletionSource<object> firstOutput = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            string output = string.Empty;
            Task<bool> checkTerminalOutputAsync = Task.Run(async () =>
            {
                byte[] buffer = new byte[4096];
                Regex ansiRegex = new Regex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))");

                while (!this.CancellationToken.IsCancellationRequested && !processExitedTcs.Task.IsCompleted)
                {
                    int count = await PseudoTerminal.ReaderStream.ReadAsync(buffer, 0, buffer.Length, this.CancellationToken);
                    if (count == 0)
                    {
                        break;
                    }

                    firstOutput.TrySetResult(null);

                    output += Encoding.GetString(buffer, 0, count);
                    output = ansiRegex.Replace(output, string.Empty);
                    if (output.Contains("\n") || output.Contains("\r"))
                    {
                        output = output.Replace("\r", string.Empty).Replace("\n", string.Empty);

                        // Parse the output before setting input ready, we need to set states if applicable
                        Terminal_ParseOutput(output);

                        // Inform that Terminal is awaiting input
                        if (output.EndsWithAny(DeterminesInput) || output.ContainsAny(DeterminesInput))
                        {
                            ReadyForInputTsk.SetResult(null);
                            ApplyInputTimeout();
                        }
                        else
                        {
                            CancelInputTimeout();
                        }

                        // Reset the output
                        output = string.Empty;
                    }
                }

                firstOutput.TrySetCanceled();
                return false;
            });

            try
            {
                await firstOutput.Task;
            }
            catch (OperationCanceledException exception)
            {
                throw new InvalidOperationException($"Could not get any output from terminal{GetTerminalExitCode()}", exception);
            }
        }

        /// <summary>
        /// Asyncronously writes a buffered string to the terminal input stream followed by an enter (0x0D) byte, then asyncronously flush
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public async Task SendCommand(string command)
        {
            if (!CancellationToken.IsCancellationRequested)
            {
                Log.Verbose($"Sending Terminal Command -> {(!command.StartsWith("login ") ? command : "login <REDACTED> <REDACTED>")}");
                ReadyForInputTsk = new TaskCompletionSource<object?>();
                byte[] commandBuffer = Encoding.GetBytes(command);
                await PseudoTerminal.WriterStream.WriteAsync(commandBuffer, 0, commandBuffer.Length, this.CancellationToken);
                await PseudoTerminal.WriterStream.WriteAsync(new byte[] { 0x0D }, 0, 1, this.CancellationToken);
                await PseudoTerminal.WriterStream.FlushAsync();
            }
        }

        /// <summary>
        /// Output handler for Terminal Process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public virtual void Terminal_ParseOutput(string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                Log.Verbose($"Base Terminal: {data}");
            }
        }

        /// <summary>
        /// An instance of Terminal should really only be used once for simplification, we all using tags by implementing IDisposable.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        PseudoTerminal.Dispose();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("An error occured whilst trying to dispose of the terminal", ex);
                    }
                }

                disposedValue = true;
            }
        }

        /// <summary>
        /// IDisposable Implementation
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(!disposedValue);
            GC.SuppressFinalize(this);
        }
    }
}
