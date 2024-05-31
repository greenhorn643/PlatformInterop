using AsyncToolkit;
using Nito.Collections;
using PlatformInterop.Shared;

namespace PlatformInterop.Host.Implementation;

internal class InteropServer<TClientInterface>(
	TClientInterface client,
	IInteropChannel channel,
	IInteropHostSerializer serializer) : IAsyncRunnable
{
	private readonly Deque<byte> buffer = [];
	private readonly byte[] tmpBuffer = new byte[1024];

	public Func<Task> Runnable => Run;

	public async Task Run()
	{
		try
		{
			while (true)
			{
				int nBytes = await channel.ReceiveAsync(tmpBuffer);

				if (nBytes == 0)
				{
					throw new PlatformInteropException("channel terminated");
				}

				buffer.InsertRange(buffer.Count, tmpBuffer[..nBytes]);

				while (true)
				{
					var rCid = serializer.DeserializeCallerId(buffer);

					if (rCid.ResultType == DeserializationResultType.InsufficientData)
					{
						break;
					}

					if (rCid.Value! == "dispose")
					{
						await channel.SendAsync(
							SerializeSuccess(
								callerId: "disposed",
								null,
								typeof(void)));
						return;
					}

					var rReq = serializer.DeserializeRequest(buffer);

					if (rReq.ResultType != DeserializationResultType.Success)
					{
						throw new PlatformInteropException("deserialize failed on second passthrough");
					}

					(var req, var methodInfo) = rReq.Value;

					var rt = methodInfo.ReturnType.GetTaskType()
						?? throw new PlatformInteropException($"attempted to call synchronous method {methodInfo.Name}; only asyncronous methods are supported");

					try
					{
						var t = (Task)methodInfo.Invoke(client, req.Args)!;
						await t;

						object? value = null;

						if (rt != typeof(void))
						{
							var resultProperty = t.GetType().GetProperty("Result")!;
							value = resultProperty.GetValue(t);
						}

						await channel.SendAsync(SerializeSuccess(req.CallerId, value, rt));
					}
					catch (Exception ex)
					{
						await channel.SendAsync(SerializeError(req.CallerId, ex.Message, rt));
					}
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}

	private byte[] SerializeSuccess(string callerId, object? value, Type returnType)
	{
		var resp = new InteropResponse
		{
			CallerId = callerId,
			IsSuccess = true,
			ErrorMessage = null,
			Value = value,
		};

		return serializer.SerializeResponse(resp, returnType);
	}

	private byte[] SerializeError(string callerId, string errorMessage, Type returnType)
	{
		var resp = new InteropResponse
		{
			CallerId = callerId,
			IsSuccess = false,
			ErrorMessage = errorMessage,
			Value = null,
		};

		return serializer.SerializeResponse(resp, returnType);
	}
}
