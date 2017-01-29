using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VSCodeDebug;

class Program
{
    static void Main(string[] args)
    {
        // Launch OpenDebugAD7 adapter
        var startInfo = new ProcessStartInfo();
        startInfo.FileName = @"C:\Users\jcouv\.vscode\extensions\ms-vscode.csharp-1.6.2\.debugger\OpenDebugAD7.exe";
        startInfo.CreateNoWindow = true;
        startInfo.RedirectStandardInput = true;
        startInfo.RedirectStandardOutput = true;
        int exitCode;

        // Run the external process & wait for it to finish
        using (Process proc = Process.Start(startInfo))
        {
            // Start chatting with it
            var client = new DebugClient();
            Task running = client.Start(proc.StandardOutput.BaseStream, proc.StandardInput.BaseStream);
            client.SendMessage(new InitializeRequest(""));
            client.SendMessage(new LaunchRequest(""));

            proc.WaitForExit();

            // Retrieve the app's exit code
            exitCode = proc.ExitCode;
        }
    }
}

public class DebugClient : ProtocolClient
{
    public DebugClient() : base()
    {
        TRACE = true;
    }

    protected override void DispatchEvent(Event @event)
    {
    }

    protected override void DispatchResponse(Response response)
    {
    }
}
