using System.IO.Pipes;

namespace PlatformInterop.Shared.Implementation.AnonymousPipeChannel;

internal class HostChannel(
	AnonymousPipeClientStream inputStream,
	AnonymousPipeClientStream outputStream) : IInteropChannel
{
	public ValueTask<int> ReceiveAsync(Memory<byte> buffer)
	{
		return inputStream.ReadAsync(buffer);
	}

	public ValueTask SendAsync(ReadOnlyMemory<byte> bytes)
	{
		return outputStream.WriteAsync(bytes);
	}
}
