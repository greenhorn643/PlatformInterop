using PlatformInterop.Host.Implementation;
using PlatformInterop.Shared;

namespace PlatformInterop.Host;

public static class InteropHost
{
	public static async Task Run<T>(T client, params string[] args)
	{
		var serializer = HostFactory.CreateHostSerializer<T>();

		(var channel, var disposable) = HostFactory.CreateHostChannel(args[0], args[1]);

		using var _ = disposable;

		var server = new InteropServer<T>(client, channel, serializer);

		await server.Run();
	}
}
