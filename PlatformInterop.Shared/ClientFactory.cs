using PlatformInterop.Shared.Implementation.AnonymousPipeChannel;
using PlatformInterop.Shared.Implementation.JsonInteropSerializer;
using System.Diagnostics;
using System.IO.Pipes;

namespace PlatformInterop.Shared;

public static class ClientFactory
{
	private static void AttachHostConsoleLogging(Process hostProcess)
	{
		void LogStdOut()
		{
			while (true)
			{
				string? line = hostProcess.StandardOutput.ReadLine();
				if (line == null)
				{
					break;
				}

				Console.WriteLine($"PLATFORM INTEROP HOST: {line}");
			}
		}

		void LogStdError()
		{
			while (true)
			{
				string? line = hostProcess.StandardError.ReadLine();
				if (line == null)
				{
					break;
				}

				Console.Error.WriteLine($"PLATFORM INTEROP HOST (Error): {line}");
			}
		}

		new Thread(LogStdOut)
		{
			IsBackground = true
		}.Start();

		new Thread(LogStdError)
		{
			IsBackground = true
		}.Start();
	}

	public static (IInteropChannel, IDisposable) CreateClientChannel(string hostExecutable)
	{
		var hostProcess = new Process();
		hostProcess.StartInfo.FileName = hostExecutable;

		var inputStream = new AnonymousPipeServerStream(
			PipeDirection.In, HandleInheritability.Inheritable);

		var outputStream = new AnonymousPipeServerStream(
			PipeDirection.Out, HandleInheritability.Inheritable);

		hostProcess.StartInfo.Arguments =
			inputStream.GetClientHandleAsString()
			+ " " + outputStream.GetClientHandleAsString();

		hostProcess.StartInfo.RedirectStandardOutput = true;
		hostProcess.StartInfo.RedirectStandardError = true;

		hostProcess.Start();

		AttachHostConsoleLogging(hostProcess);

		return (
			new ClientChannel(inputStream, outputStream),
			new ChannelDisposable(hostProcess, inputStream, outputStream));
	}

	public static IInteropClientSerializer CreateClientSerializer()
	{
		return new ClientSerializer();
	}

	private class ChannelDisposable(
		Process hostProcess,
		AnonymousPipeServerStream inputStream,
		AnonymousPipeServerStream outputStream) : IDisposable
	{
		private bool disposed = false;

		public void Dispose()
		{
			if (!disposed)
			{
				hostProcess.Kill();
				hostProcess.Dispose();
				inputStream.Dispose();
				outputStream.Dispose();
				disposed = true;
			}
		}
	}
}
