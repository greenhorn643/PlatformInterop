using PlatformInterop.Shared.Implementation.AnonymousPipeChannel;
using PlatformInterop.Shared.Implementation.JsonInteropSerializer;
using System.IO.Pipes;

namespace PlatformInterop.Shared;

public static class HostFactory
{
	public static IInteropHostSerializer CreateHostSerializer<TClientInterface>()
	{
		var methodLocator = new MethodLocator(typeof(TClientInterface));
		return new HostSerializer(methodLocator);
	}

	public static (IInteropChannel, IDisposable) CreateHostChannel(
		string outputStreamHandle,
		string inputStreamHandle)
	{
		var inputStream = new AnonymousPipeClientStream(
			PipeDirection.In, inputStreamHandle);
		var outputStream = new AnonymousPipeClientStream(
			PipeDirection.Out, outputStreamHandle);

		return (
			new HostChannel(inputStream, outputStream),
			new ChannelDisposable(inputStream, outputStream));
	}

	private class ChannelDisposable(
		AnonymousPipeClientStream inputStream,
		AnonymousPipeClientStream outputStream) : IDisposable
	{
		private bool disposed = false;

		public void Dispose()
		{
			if (!disposed)
			{
				inputStream.Dispose();
				outputStream.Dispose();
				disposed = true;
			}
		}
	}
}
