/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace VSCodeDebug
{
	// ---- Types -------------------------------------------------------------------------

	public class Message {
		public int id { get; }
		public string format { get; }
		public dynamic variables { get; }
		public dynamic showUser { get; }
		public dynamic sendTelemetry { get; }

		public Message(int id, string format, dynamic variables = null, bool user = true, bool telemetry = false) {
			this.id = id;
			this.format = format;
			this.variables = variables;
			this.showUser = user;
			this.sendTelemetry = telemetry;
		}
	}

	public class StackFrame
	{
		public int id { get; }
		public Source source { get; }
		public int line { get; }
		public int column { get; }
		public string name { get; }

		public StackFrame(int id, string name, Source source, int line, int column) {
			this.id = id;
			this.name = name;
			this.source = source;
			this.line = line;
			this.column = column;
		}
	}

	public class Scope
	{
		public string name { get; }
		public int variablesReference { get; }
		public bool expensive { get; }

		public Scope(string name, int variablesReference, bool expensive = false) {
			this.name = name;
			this.variablesReference = variablesReference;
			this.expensive = expensive;
		}
	}

	public class Variable
	{
		public string name { get; }
		public string value { get; }
		public int variablesReference { get; }

		public Variable(string name, string value, int variablesReference = 0) {
			this.name = name;
			this.value = value;
			this.variablesReference = variablesReference;
		}
	}

	public class Thread
	{
		public int id { get; }
		public string name { get; }

		public Thread(int id, string name) {
			this.id = id;
			if (name == null || name.Length == 0) {
				this.name = string.Format("Thread #{0}", id);
			}
			else {
				this.name = name;
			}
		}
	}

	public class Source
	{
		public string name { get; }
		public string path { get; }
		public int sourceReference { get; }

		public Source(string name, string path, int sourceReference = 0) {
			this.name = name;
			this.path = path;
			this.sourceReference = sourceReference;
		}

		public Source(string path, int sourceReference = 0) {
			this.name = Path.GetFileName(path);
			this.path = path;
			this.sourceReference = sourceReference;
		}
	}

	public class Breakpoint
	{
		public bool verified { get; }
		public int line { get; }

		public Breakpoint(bool verified, int line) {
			this.verified = verified;
			this.line = line;
		}
	}

	// ---- Events -------------------------------------------------------------------------

	public class InitializedEvent : Event
	{
		public InitializedEvent()
			: base("initialized") { }
	}

	public class StoppedEvent : Event
	{
		public StoppedEvent(int tid, string reasn, string txt = null)
			: base("stopped", new {
				threadId = tid,
				reason = reasn,
				text = txt
			}) { }
	}

	public class ExitedEvent : Event
	{
		public ExitedEvent(int exCode)
			: base("exited", new { exitCode = exCode } ) { }
	}

	public class TerminatedEvent : Event
	{
		public TerminatedEvent()
			: base("terminated") {	}
	}

	public class ThreadEvent : Event
	{
		public ThreadEvent(string reasn, int tid)
			: base("thread", new {
				reason = reasn,
				threadId = tid
			}) { }
	}

	public class OutputEvent : Event
	{
		public OutputEvent(string cat, string outpt)
			: base("output", new {
				category = cat,
				output = outpt
			}) { }
	}

	// ---- Response -------------------------------------------------------------------------

	public class Capabilities : ResponseBody {

		public bool supportsConfigurationDoneRequest;
		public bool supportsFunctionBreakpoints;
		public bool supportsConditionalBreakpoints;
		public bool supportsEvaluateForHovers;
		public dynamic[] exceptionBreakpointFilters;
	}

	public class ErrorResponseBody : ResponseBody {

		public Message error { get; }

		public ErrorResponseBody(Message error) {
			this.error = error;
		}
	}

	public class StackTraceResponseBody : ResponseBody
	{
		public StackFrame[] stackFrames { get; }

		public StackTraceResponseBody(List<StackFrame> frames = null) {
			if (frames == null)
				stackFrames = new StackFrame[0];
			else
				stackFrames = frames.ToArray<StackFrame>();
		}
	}

	public class ScopesResponseBody : ResponseBody
	{
		public Scope[] scopes { get; }

		public ScopesResponseBody(List<Scope> scps = null) {
			if (scps == null)
				scopes = new Scope[0];
			else
				scopes = scps.ToArray<Scope>();
		}
	}

	public class VariablesResponseBody : ResponseBody
	{
		public Variable[] variables { get; }

		public VariablesResponseBody(List<Variable> vars = null) {
			if (vars == null)
				variables = new Variable[0];
			else
				variables = vars.ToArray<Variable>();
		}
	}

	public class ThreadsResponseBody : ResponseBody
	{
		public Thread[] threads { get; }

		public ThreadsResponseBody(List<Thread> vars = null) {
			if (vars == null)
				threads = new Thread[0];
			else
				threads = vars.ToArray<Thread>();
		}
	}

	public class EvaluateResponseBody : ResponseBody
	{
		public string result { get; }
		public int variablesReference { get; }

		public EvaluateResponseBody(string value, int reff = 0) {
			result = value;
			variablesReference = reff;
		}
	}

	public class SetBreakpointsResponseBody : ResponseBody
	{
		public Breakpoint[] breakpoints { get; }

		public SetBreakpointsResponseBody(List<Breakpoint> bpts = null) {
			if (bpts == null)
				breakpoints = new Breakpoint[0];
			else
				breakpoints = bpts.ToArray<Breakpoint>();
		}
	}
}