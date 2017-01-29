using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace VSCodeDebug
{
    // Useful reference: for the protocol
    // C:\Users\jcouv\.vscode\extensions\ms-vscode.csharp-1.6.2\node_modules\vscode-debugprotocol\lib\debugProtocol.d.ts

    public class InitializeRequest : Request
    {
        public InitializeRequest(InitializeRequestArguments arg) : base("initialize", arg) { }
    }

    public class InitializeRequestArguments
    {
        public string adapterID = "coreclr";
        public string pathFormat = "path";
        public bool ShouldSerializePathFormat() => pathFormat != null && pathFormat != "path";
    }

    public class LaunchRequest : Request
    {
        public LaunchRequest(LaunchArguments arg) : base("launch", arg) { }
    }

    public class LaunchArguments
    {
        public string program = "";
        public string cwd = "";
        public string[] args = null;
        public string name = "bob";
        public string type = "coreclr";
        public string request = "launch";
        public bool stopAtEntry = true;
        public bool externalConsole = false;
        public LoggingOptions logging;
        public bool ShouldSerializeLogging => logging != null;

        // env
        // launchBrowser
        // sourceFileMap
        // justMyCode
        // launchOptionType
        // symbolPath
    }
    public class LoggingOptions
    {
        public bool exceptions = true;
        public bool ShouldSerializeExceptions()  => !exceptions;

        public bool moduleLoad = true;
        public bool ShouldSerializeModuleLoad() => !moduleLoad;
        public bool programOutput = true;
        public bool ShouldSerializeProgramOutput() => !programOutput;
        public bool engineLogging = false;
        public bool ShouldSerializeEngineLogging() => engineLogging;
        public bool trace = false;
        public bool ShouldSerializeTrace() => trace;
        public bool traceResponse = false;
        public bool ShouldSerializeTraceResponse() { return traceResponse; }
    }

    public abstract class ProtocolClient
    {
        public bool TRACE;
        protected const int BUFFER_SIZE = 4096;
        protected const string TWO_CRLF = "\r\n\r\n";
        protected static readonly Regex CONTENT_LENGTH_MATCHER = new Regex(@"Content-Length: (\d+)");
        protected static readonly Encoding Encoding = System.Text.Encoding.UTF8;
        private Stream _outputStream;
        private int _sequenceNumber;
        private ByteBuffer _rawData;
        private int _bodyLength;
        private bool _stopRequested;

        public ProtocolClient()
        {
            _sequenceNumber = 1;
            _bodyLength = -1;
            _rawData = new ByteBuffer();
        }

        public async Task Start(Stream inputStream, Stream outputStream)
        {
            _outputStream = outputStream;

            byte[] buffer = new byte[BUFFER_SIZE];

            _stopRequested = false;
            while (!_stopRequested)
            {
                var read = await inputStream.ReadAsync(buffer, 0, buffer.Length);

                if (read == 0)
                {
                    // end of stream
                    break;
                }

                if (read > 0)
                {
                    _rawData.Append(buffer, read);
                    ProcessData();
                }
            }
        }

        public void Stop()
        {
            _stopRequested = true;
        }

        protected abstract void DispatchEvent(Event @event);
        private ConcurrentDictionary<int, TaskCompletionSource<Response>> board =
            new ConcurrentDictionary<int, TaskCompletionSource<Response>>();

        public async Task<InitializeResponse> Initialize(InitializeRequest request)
        {
            object response = await Request(request);
            return (InitializeResponse)response;
        }

        public async Task<Response> Request(Request request)
        {
            request.seq = _sequenceNumber++;
            var task = new TaskCompletionSource<Response>();
            board[request.seq] = task;

            SendMessage(request);
            return await task.Task;
        }

        private void SendMessage(ProtocolMessage message)
        {
            if (message.seq == 0)
            {
                message.seq = _sequenceNumber++;
            }

            if (TRACE && message.type == "request")
            {
                Console.Error.WriteLine(string.Format("Request {0}: {1}", ((Request)message).command,
                    JsonConvert.SerializeObject(message)));
            }

            var data = ConvertToBytes(message);

            try
            {
                _outputStream.Write(data, 0, data.Length);
                _outputStream.Flush();
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private static byte[] ConvertToBytes(ProtocolMessage request)
        {
            var asJson = JsonConvert.SerializeObject(request);
            byte[] jsonBytes = Encoding.GetBytes(asJson);

            string header = string.Format("Content-Length: {0}{1}", jsonBytes.Length, TWO_CRLF);
            byte[] headerBytes = Encoding.GetBytes(header);

            byte[] data = new byte[headerBytes.Length + jsonBytes.Length];
            System.Buffer.BlockCopy(headerBytes, 0, data, 0, headerBytes.Length);
            System.Buffer.BlockCopy(jsonBytes, 0, data, headerBytes.Length, jsonBytes.Length);

            return data;
        }

        private void ProcessData()
        {
            while (true)
            {
                if (_bodyLength >= 0)
                {
                    if (_rawData.Length >= _bodyLength)
                    {
                        var buf = _rawData.RemoveFirst(_bodyLength);

                        _bodyLength = -1;

                        Dispatch(Encoding.GetString(buf));

                        continue;   // there may be more complete messages to process
                    }
                }
                else
                {
                    string s = _rawData.GetString(Encoding);
                    var idx = s.IndexOf(TWO_CRLF);
                    if (idx != -1)
                    {
                        Match m = CONTENT_LENGTH_MATCHER.Match(s);
                        if (m.Success && m.Groups.Count == 2)
                        {
                            _bodyLength = Convert.ToInt32(m.Groups[1].ToString());

                            _rawData.RemoveFirst(idx + TWO_CRLF.Length);

                            continue;   // try to handle a complete message
                        }
                    }
                }
                break;
            }
        }

        private void Dispatch(string msg)
        {
            var message = JsonConvert.DeserializeObject<ProtocolMessage>(msg);
            if (message == null)
            {
                throw new Exception(msg);
            }

            if (message.type == "response")
            {
                var response = JsonConvert.DeserializeObject<Response>(msg);
                TaskCompletionSource<Response> task;
                board.TryRemove(response.request_seq, out task);

                if (TRACE)
                    Console.Error.WriteLine(string.Format("Response {0}: {1}", response.command, msg));

                if (response.command == "initialize")
                {
                    var initializeResponse = JsonConvert.DeserializeObject<InitializeResponse>(msg);

                    if (TRACE)
                        // Console.Error.WriteLine(string.Format(" R {0}: {1} - {2}", response.command, JsonConvert.SerializeObject(initializeResponse), msg));

                        if (task != null)
                        {
                            task.SetResult(initializeResponse);
                        }
                }
                else
                {
                    if (task != null)
                    {
                        task.SetResult(response);
                    }
                }
            }

            if (message.type == "event")
            {
                var @event = JsonConvert.DeserializeObject<Event>(msg);
                if (TRACE && @event.eventType == "output")
                {
                    var output = JsonConvert.DeserializeObject<OutputEvent>(msg);
                    Console.Error.WriteLine(output.body.output);
                }
                else if (TRACE)
                {
                    Console.Error.WriteLine(string.Format("Event {0}: {1}", @event.eventType, JsonConvert.SerializeObject(@event.body)));
                }
                DispatchEvent(@event);
            }
        }
    }
}