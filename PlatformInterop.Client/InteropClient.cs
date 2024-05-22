using PlatformInterop.Client.Implementation;
using PlatformInterop.Shared;

namespace PlatformInterop.Client;

public static class InteropClient
{
	public static (TClientInterface, IDisposable) CreateClient<TClientInterface>(string hostExecutable)
	{
		var clientType = InteropClientWeaver.CreateClientType(typeof(TClientInterface))
			?? throw new PlatformInteropException($"failed to create type based on interface {typeof(TClientInterface).Name}");

		(var channel, var disposable) = ClientFactory.CreateClientChannel(hostExecutable);

		var serializer = ClientFactory.CreateClientSerializer();

		var client = Activator.CreateInstance(clientType, channel, serializer)
			?? throw new PlatformInteropException($"failed to instantiate instance of type based on interface {typeof(TClientInterface).Name}");

		return ((TClientInterface)client, disposable);
	}
}