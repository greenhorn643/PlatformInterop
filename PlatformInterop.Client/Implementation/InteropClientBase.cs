using PlatformInterop.Shared;
using System.Collections.Concurrent;
using System.Reflection;

namespace PlatformInterop.Client.Implementation;

public class InteropClientBase(IInteropChannel channel, IInteropClientSerializer interopSerializer)
{
	private static readonly ConcurrentDictionary<string, InteropMethodInfo> cache = [];

	public static string RegisterMethod(
		MethodInfo method)
	{
		var methodId = Guid.NewGuid().ToString();

		var metaData = new InteropMethodInfo
		{
			MethodName = method.Name,
			ArgumentTypes = [.. method.GetParameters().Select(_ => _.ParameterType)],
			ReturnType = method.ReturnType,
		};

		cache[methodId] = metaData;
		return methodId;
	}

	public async Task<object?> RequestAsync(string methodId, object[] args)
	{
		if (!cache.TryGetValue(methodId, out var methodInfo))
		{
			throw new ArgumentException(
				"no method has been registered for this id",
				nameof(methodId));
		}

		if (args.Length != methodInfo.ArgumentTypes.Length)
		{
			throw new ArgumentException(
				$"expected {methodInfo.ArgumentTypes.Length} arguments; found {args.Length}",
				nameof(args));
		}

		var rt = methodInfo.ReturnType.GetTaskType()
			?? throw new PlatformInteropException($"attempted to call synchronous method {methodInfo.MethodName}; only asyncronous methods are supported");

		var bytes = interopSerializer.SerializeRequest(methodInfo, args);
		await channel.SendAsync(bytes);

		List<byte> buffer = [];
		byte[] tmpBuffer = new byte[1024];

		while (true)
		{
			int nBytes = await channel.ReceiveAsync(tmpBuffer);

			if (nBytes == 0)
			{
				throw new PlatformInteropException("connection to host process broken");
			}

			buffer.AddRange(tmpBuffer.AsSpan()[..nBytes]);
			var r = interopSerializer.DeserializeResponse(buffer, rt);

			if (r.ResultType == DeserializationResultType.InsufficientData)
			{
				continue;
			}

			var response = r.Value!;

			if (response.IsSuccess)
			{
				return response.Value;
			}
			else
			{
				throw new PlatformInteropException(response.ErrorMessage ?? "(no error message)");
			}
		}
	}
}
