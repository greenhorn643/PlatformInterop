namespace PlatformInterop.Shared;

public interface IInteropChannel
{
	ValueTask SendAsync(ReadOnlyMemory<byte> bytes);

	ValueTask<int> ReceiveAsync(Memory<byte> buffer);
}