using Pty.Net;
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
    internal abstract class Terminal : IDisposable, ITerminal
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
        internal event EventHandler Finished;

        public string[] DeterminesInput { get; set; } = new string[] { };

        public TaskCompletionSource<object?> ReadyForInputTsk { get; set; }

        /// <summary>
        /// OS Native PseudoTerminal Object
        /// </summary>
        protected IPtyConnection PseudoTerminal { get; set; }

        /// <summary>
        /// The executable path for Terminal in Windows
        /// </summary>
        public virtual string ExecutablePath { get; }

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
        protected Terminal(int timeoutOnAwaitingInputMilliseconds)
        {
            CancellationTokenSource = new CancellationTokenSource();
            CancellationToken = CancellationTokenSource.Token;

            _timeoutOnAwaitingInput = timeoutOnAwaitingInputMilliseconds;
        }

        /// <summary>
        /// Instantiate a new terminal containing Terminal natively, and return upon input available
        /// </summary>
        /// <param name="timeoutOnAwaitingInputMilliseconds"></param>
        /// <returns></returns>
        internal static async Task<ITerminal> Instantiate<T>(TerminalStartUpOptions terminalStartUpOptions)
        {
            ITerminal _terminal = (ITerminal)Activator.CreateInstance(typeof(T), terminalStartUpOptions.InputTimeout);

            Console.WriteLine("Terminal Instantiated");

            try
            {
                await _terminal.ConnectToTerminal(terminalStartUpOptions.Name);

                Console.WriteLine("Terminal Connected");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Could not start the terminal", ex);
            }

            if (Utility.OperatingSystemHelper.IsWindows())
            {
                await _terminal.SendCommand(_terminal.ExecutablePath);
            }
            else
            {
                _terminal.ReadyForInputTsk = new TaskCompletionSource<object?>();
                //await Terminal.SendCommand(LinExecutablePath);
            }

            Console.WriteLine("Terminal Waiting For Input");

            await _terminal.ReadyForInputTsk.Task;

            Console.WriteLine("Terminal Ready");

            return _terminal;
        }

        /// <summary>
        /// Sends a command to Terminal requesting a peaceful quit.
        /// Kills the process and pseudoterminal after x seconds if it does not exit peacefully.
        /// </summary>
        /// <returns></returns>
        protected void Shutdown(int timeout = 10)
        {
            System.Timers.Timer aTimer = new System.Timers.Timer(1000);
            // Hook up the Elapsed event for the timer. 
            int seconds = timeout + 1;
            aTimer.Elapsed += delegate { Console.WriteLine($"Waiting for steam to close peacefully.. {--seconds}"); };
            aTimer.AutoReset = true;
            aTimer.Enabled = true;

            // If the terminal hasn't exited, we should wait a short amount of time and then kill it if it's still alive
            if (PseudoTerminal.WaitForExit(seconds * 1000))
            {
                // cancel the timer
                aTimer.Enabled = false;
            }
            else
            {
                // cancel the timer
                aTimer.Enabled = false;
                // Kill the terminal process containing Terminal
                PseudoTerminal?.Kill();
            }
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
                    Console.WriteLine($"Exceeded Awaiting Input Timeout: {_timeoutOnAwaitingInput}ms");
                    CancellationTokenSource.Cancel();
                }
            }, ReadyForInputTimeoutTskCts.Token);
        }

        /// <summary>
        /// Spawn a new Pseudoterminal, begin asyncronously reading its input and output, apply terminal events
        /// </summary>
        /// <returns></returns>
        public async Task ConnectToTerminal(string _terminalName)
        {
            string app = Utility.OperatingSystemHelper.IsWindows() ? Path.Combine(Environment.SystemDirectory, "cmd.exe") : "Terminal";
            PtyOptions options = new PtyOptions
            {
                Name = _terminalName,
                // TODO this should be quite long, and cover anything that Terminal can spit out in a single line + the current directory length
                Cols = Environment.CurrentDirectory.Length + app.Length + 200,
                // we want it line by line, no more than that
                Rows = 1,
                Cwd = Program.WorkingDirectory,
                App = app
            };

            PseudoTerminal = await PtyProvider.SpawnAsync(options, this.CancellationToken);

            var processExitedTcs = new TaskCompletionSource<uint>();
            PseudoTerminal.ProcessExited += (sender, e) =>
            {
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
                var buffer = new byte[4096];
                var ansiRegex = new Regex(@"[\u001B\u009B][[\]()#;?]*(?:(?:(?:[a-zA-Z\d]*(?:;[a-zA-Z\d]*)*)?\u0007)|(?:(?:\d{1,4}(?:;\d{0,4})*)?[\dA-PRZcf-ntqry=><~]))");

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
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
