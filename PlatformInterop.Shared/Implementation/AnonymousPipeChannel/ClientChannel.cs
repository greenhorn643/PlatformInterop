using System.IO.Pipes;

namespace PlatformInterop.Shared.Implementation.AnonymousPipeChannel;

internal class ClientChannel(
	AnonymousPipeServerStream inputStream,
	AnonymousPipeServerStream outputStream) : IInteropChannel
{
	public int Receive(byte[] buffer) => inputStream.Read(buffer);

	public async Task<int> ReceiveAsync(byte[] buffer)
	{
		return await inputStream.ReadAsync(buffer)
			.ConfigureAwait(false);
	}

	public void Send(byte[] bytes)
	{
		outputStream.Write(bytes);
		outputStream.Flush();
	}

	public async Task SendAsync(byte[] bytes)
	{
		await outputStream.WriteAsync(bytes)
			.ConfigureAwait(false);
		await outputStream.FlushAsync()
			.ConfigureAwait(false);
	}
}
