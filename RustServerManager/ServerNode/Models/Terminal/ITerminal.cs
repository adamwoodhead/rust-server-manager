using System.Threading.Tasks;

namespace ServerNode.Models.Terminal
{
    public interface ITerminal
    {
        string[] DeterminesInput { get; set; }
        string ExecutablePath { get; set; }
        TaskCompletionSource<object?> ReadyForInputTsk { get; set; }
        Task ConnectToTerminal(string name, string workingDir = null, string customExectutable = null, string[] commandline = null);
        Task SendCommand(string command);
        void Terminal_ParseOutput(string data);
        void Dispose();
    }
}