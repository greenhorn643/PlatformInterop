namespace PlatformInterop.Shared;

public interface IInteropChannel
{
	Task SendAsync(byte[] bytes);
	void Send(byte[] bytes);

	Task<int> ReceiveAsync(byte[] buffer);
	int Receive(byte[] buffer);
}