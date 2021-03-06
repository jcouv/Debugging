using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VSCodeDebug;

class Program
{
    static void Main()
    {
        MainImpl().GetAwaiter().GetResult();
    }

    static async Task MainImpl()
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
            new System.Threading.Thread(() => running.GetAwaiter().GetResult());

            InitializeResponse initResponse = await client.Initialize(new InitializeRequest(new InitializeRequestArguments()));

            Response launchResponse = await client.Request(new LaunchRequest(new LaunchArguments()
            {
                program = @"C:\Users\jcouv\.babun\cygwin\home\jcouv\issues\hello-world\bin\Debug\netcoreapp1.0\hello-world.dll",
                cwd = @"C:\Users\jcouv\.babun\cygwin\home\jcouv\issues\hello-world\bin\Debug\netcoreapp1.0",
                logging = new LoggingOptions() { engineLogging = true, trace = true, traceResponse = true }
            }));

            await client.Initialized();

            Response response;
            response = await client.Request(new SetExceptionBreakpointsRequest(new SetExceptionBreakpointsArguments()));
            response = await client.Request(new ConfigurationDoneRequest());
            response = await client.Request(new ThreadsRequest());
            // response = await client.Request(new NextRequest(new NextArguments()));
            // response = await client.Request(new ThreadsRequest());
            // response = await client.Request(new StackTraceRequest(new StackTraceArguments()));

            proc.WaitForExit();

            // Retrieve the app's exit code
            exitCode = proc.ExitCode;
        }
    }
}

public class DebugClient : ProtocolClient
{
    TaskCompletionSource<bool> initialized = new TaskCompletionSource<bool>();
    public DebugClient() : base()
    {
        TRACE = true;
    }

    protected override void DispatchEvent(Event @event)
    {
        switch(@event.eventType)
        {
            case "initialized":
                initialized.SetResult(true);
                break;
        }
    }

    public async Task<bool> Initialized()
    {
        return await initialized.Task;
    }
}
