using System.IO.Pipes;

namespace PlatformInterop.Shared.Implementation.AnonymousPipeChannel;

internal class ClientChannel(
	AnonymousPipeServerStream inputStream,
	AnonymousPipeServerStream outputStream) : IInteropChannel
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
