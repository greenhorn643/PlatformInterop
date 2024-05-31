using System.IO.Pipes;

namespace PlatformInterop.Shared.Implementation.AnonymousPipeChannel;

internal class HostChannel(
	AnonymousPipeClientStream inputStream,
	AnonymousPipeClientStream outputStream) : IInteropChannel
{
	public async Task<int> ReceiveAsync(byte[] buffer)
	{
		return await inputStream.ReadAsync(buffer)
			.ConfigureAwait(false);
	}

	public async Task SendAsync(byte[] bytes)
	{
		await outputStream.WriteAsync(bytes)
			.ConfigureAwait(false);
		await outputStream.FlushAsync()
			.ConfigureAwait(false);
	}
}
