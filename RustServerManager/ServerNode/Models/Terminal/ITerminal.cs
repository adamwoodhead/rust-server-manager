using System.Threading.Tasks;

namespace ServerNode.Models.Terminal
{
    internal interface ITerminal
    {
        string[] DeterminesInput { get; set; }
        string ExecutablePath { get; }
        TaskCompletionSource<object?> ReadyForInputTsk { get; set; }
        Task ConnectToTerminal(string name);
        Task SendCommand(string command);
        void Terminal_ParseOutput(string data);
        void Dispose();
    }
}