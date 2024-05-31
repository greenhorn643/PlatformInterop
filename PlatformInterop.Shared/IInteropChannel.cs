namespace PlatformInterop.Shared;

public interface IInteropChannel
{
	Task SendAsync(byte[] bytes);

	Task<int> ReceiveAsync(byte[] buffer);
}