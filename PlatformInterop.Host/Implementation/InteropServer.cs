using PlatformInterop.Shared;

namespace PlatformInterop.Host.Implementation;

internal class InteropServer<TClientInterface>(
	TClientInterface client,
	IInteropChannel channel,
	IInteropHostSerializer serializer)
{
	public async Task Run()
	{
		while (true)
		{
			await HandleRequest().ConfigureAwait(false);
		}
	}

	private async Task HandleRequest()
	{
		byte[] tmpBuffer = new byte[1024];
		List<byte> buffer = [];

		while (true)
		{
			int nBytes = await channel.ReceiveAsync(tmpBuffer)
				.ConfigureAwait(false);

			if (nBytes == 0)
			{
				throw new PlatformInteropException("channel terminated");
			}

			buffer.AddRange(tmpBuffer.AsSpan()[..nBytes]);

			var r = serializer.DeserializeRequest(buffer);

			if (r.ResultType == DeserializationResultType.InsufficientData)
			{
				continue;
			}

			(var methodInfo, var args) = r.Value;

			var rt = methodInfo.ReturnType.GetTaskType()
				?? throw new PlatformInteropException($"attempted to call synchronous method {methodInfo.Name}; only asyncronous methods are supported");

			try
			{
				var t = (Task)methodInfo.Invoke(client, args)!;
				await t.ConfigureAwait(false);

				object? value = null;

				if (rt != typeof(void))
				{
					var resultProperty = t.GetType().GetProperty("Result")!;
					value = resultProperty.GetValue(t);
				}

				await channel.SendAsync(SerializeSuccess(value, rt))
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				await channel.SendAsync(SerializeError(ex.Message, rt))
					.ConfigureAwait(false);
			}

			break;
		}
	}

	private byte[] SerializeSuccess(object? value, Type returnType)
	{
		var resp = new InteropResponse
		{
			IsSuccess = true,
			ErrorMessage = null,
			Value = value,
		};

		return serializer.SerializeResponse(resp, returnType);
	}

	private byte[] SerializeError(string errorMessage, Type returnType)
	{
		var resp = new InteropResponse
		{
			IsSuccess = false,
			ErrorMessage = errorMessage,
			Value = null,
		};

		return serializer.SerializeResponse(resp, returnType);
	}

}
