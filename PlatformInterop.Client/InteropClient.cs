using AsyncToolkit;
using PlatformInterop.Client.Implementation;
using PlatformInterop.Shared;

namespace PlatformInterop.Client;

public static class InteropClient
{
	private class Disposer : IAsyncDisposable
	{
		public required IDisposable ChannelDisposable { get; set; }
		public required IAsyncDisposable ClientDisposable { get; set; }

		public async ValueTask DisposeAsync()
		{
			await ClientDisposable.DisposeAsync();
			ChannelDisposable.Dispose();
		}
	}

	public static (TClientInterface, IAsyncDisposable) CreateClient<TClientInterface>(string hostExecutable)
	{
		var clientType = InteropClientWeaver.CreateClientType(typeof(TClientInterface))
			?? throw new PlatformInteropException($"failed to create type based on interface {typeof(TClientInterface).Name}");

		(var channel, var channelDisposable) = ClientFactory.CreateClientChannel(hostExecutable);

		var serializer = ClientFactory.CreateClientSerializer();

		var client = Activator.CreateInstance(clientType, channel, serializer)
			?? throw new PlatformInteropException($"failed to instantiate instance of type based on interface {typeof(TClientInterface).Name}");

		var scheduler = new SingleThreadedTaskScheduler();

		((IAsyncRunnable)client).Run(scheduler);

		new Thread(scheduler.RunOnCurrentThread)
		{
			IsBackground = true
		}.Start();

		return (
			(TClientInterface)client,
			new Disposer
			{
				ChannelDisposable = channelDisposable,
				ClientDisposable = (IAsyncDisposable)client
			});
	}
}