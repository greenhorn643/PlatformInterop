using AsyncToolkit;
using PlatformInterop.Shared;
using System.Collections.Concurrent;
using System.Reflection;
using Buffer = ByteBuffer.ByteBuffer;

namespace PlatformInterop.Client.Implementation;

public class InteropClientBase(IInteropChannel channel, IInteropClientSerializer interopSerializer) : IAsyncRunnable, IAsyncDisposable
{
	private static readonly ConcurrentDictionary<string, InteropMethodInfo> cache = [];
	private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> pendingMethodCalls = [];
	private readonly ConcurrentDictionary<string, Type> pendingMethodReturnTypes = [];
	private readonly TaskCompletionSource pendingDispose = new();
	private bool disposed = false;

	public Func<Task> Runnable => ReceiverLoop;

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
		var callerId = Guid.NewGuid().ToString();

		var tcs = new TaskCompletionSource<object?>();

		if (!pendingMethodCalls.TryAdd(callerId, tcs))
		{
			throw new PlatformInteropException("guid collision");
		}

		await SendRequest(callerId, methodId, args);

		return await tcs.Task;
	}

	private async Task SendRequest(string callerId, string methodId, object[] args)
	{
		ObjectDisposedException.ThrowIf(disposed, this);

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

		if (!pendingMethodReturnTypes.TryAdd(callerId, rt))
		{
			throw new PlatformInteropException("guid collision");
		}

		var bytes = interopSerializer.SerializeRequest(new InteropRequest
		{
			CallerId = callerId,
			MethodInfo = methodInfo,
			Args = args,
		});

		await channel.SendAsync(bytes);
	}

	private async Task ReceiverLoop()
	{
		Buffer buffer = [];
		byte[] tmpBuffer = new byte[1024];

		while (true)
		{
			int nBytes = await channel.ReceiveAsync(tmpBuffer);

			if (nBytes == 0)
			{
				throw new PlatformInteropException("connection to host process broken");
			}

			buffer.AddRange(tmpBuffer.AsMemory()[..nBytes]);

			while (true)
			{
				var rCid = interopSerializer.DeserializeCallerId(buffer);

				if (rCid.ResultType == DeserializationResultType.InsufficientData)
				{
					break;
				}

				var callerId = rCid.Value!;

				if (callerId == "disposed")
				{
					KillPendingTasks();
					pendingDispose.SetResult();
					return;
				}

				if (!pendingMethodReturnTypes.TryRemove(callerId, out var rt))
				{
					throw new PlatformInteropException($"{nameof(pendingMethodReturnTypes)}: {nameof(callerId)} {callerId} not found");
				}

				var rResp = interopSerializer.DeserializeResponse(buffer, rt);

				if (rResp.ResultType == DeserializationResultType.InsufficientData)
				{
					throw new PlatformInteropException("second pass deserialization failure");
				}

				if (!pendingMethodCalls.TryRemove(callerId, out var tcs))
				{
					throw new PlatformInteropException($"{nameof(pendingMethodCalls)}: {nameof(callerId)} {callerId} not found");
				}

				var response = rResp.Value!;

				Task.Run(() =>
				{
					if (response.IsSuccess)
					{
						tcs.SetResult(response.Value);
					}
					else
					{
						tcs.SetException(
							new PlatformInteropException(response.ErrorMessage ?? "(no error message)"));
					}
				});
			}
		}
	}

	private void KillPendingTasks()
	{
		try
		{
			foreach (var (_, tcs) in pendingMethodCalls)
			{
				tcs.SetException(new ObjectDisposedException("InteropHost"));
			}
		}
		catch { }
	}

	public async ValueTask DisposeAsync()
	{
		if (!disposed)
		{
			disposed = true;

			var bytes = interopSerializer.SerializeRequest(new InteropRequest
			{
				CallerId = "dispose",
				MethodInfo = new InteropMethodInfo
				{
					MethodName = "Dispose",
					ArgumentTypes = [],
					ReturnType = typeof(void)
				},
				Args = [],
			});

			await channel.SendAsync(bytes);
			await pendingDispose.Task;
		}
	}
}
