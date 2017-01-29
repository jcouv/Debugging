/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System.Text;
using Newtonsoft.Json;

// see https://github.com/Microsoft/vscode-debugadapter-node/blob/master/protocol/src/debugProtocol.ts

namespace VSCodeDebug
{
    public class ProtocolMessage
    {
        public int seq;
        public string type;

        public ProtocolMessage(string typ)
        {
            type = typ;
        }
    }

    public class Request : ProtocolMessage
    {
        public string command;
        public dynamic arguments;

        public Request(string cmd, dynamic arg) : base("request")
        {
            command = cmd;
            arguments = arg;
        }
    }

    /*
	 * subclasses of ResponseBody are serialized as the body of a response.
	 * Don't change their instance variables since that will break the debug protocol.
	 */
    public class ResponseBody
    {
        // empty
    }

    public class Response : ProtocolMessage
    {
        public bool success;
        public string message;
        public int request_seq;
        public string command;
        // bool running
        // refs

        public Response() : base("response") { }
    }

    public class InitializeResponse : Response
    {
        public Capabilities body;
    }

    public class Event : ProtocolMessage
    {
        [JsonProperty(PropertyName = "event")]
        public string eventType;
        public dynamic body;

        public Event(string type, dynamic bdy = null) : base("event")
        {
            eventType = type;
            body = bdy;
        }
    }

    //--------------------------------------------------------------------------------------

    class ByteBuffer
    {
        private byte[] _buffer;

        public ByteBuffer()
        {
            _buffer = new byte[0];
        }

        public int Length
        {
            get { return _buffer.Length; }
        }

        public string GetString(Encoding enc)
        {
            return enc.GetString(_buffer);
        }

        public void Append(byte[] b, int length)
        {
            byte[] newBuffer = new byte[_buffer.Length + length];
            System.Buffer.BlockCopy(_buffer, 0, newBuffer, 0, _buffer.Length);
            System.Buffer.BlockCopy(b, 0, newBuffer, _buffer.Length, length);
            _buffer = newBuffer;
        }

        public byte[] RemoveFirst(int n)
        {
            byte[] b = new byte[n];
            System.Buffer.BlockCopy(_buffer, 0, b, 0, n);
            byte[] newBuffer = new byte[_buffer.Length - n];
            System.Buffer.BlockCopy(_buffer, n, newBuffer, 0, _buffer.Length - n);
            _buffer = newBuffer;
            return b;
        }
    }
}